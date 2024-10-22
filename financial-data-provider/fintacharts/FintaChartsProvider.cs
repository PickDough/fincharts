using System.Buffers;
using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using app_core.dto;
using app_core.service;
using AutoMapper;
using DotNext;
using DotNext.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace financial_data_provider.fintacharts;

public class FintaChartsProvider(
    string apiAddress,
    string username,
    string password,
    ILogger<FintaChartsProvider> logger
) : IAssetProvider, IAssetsHistoricalPriceProvider, IAssetRealtimePriceProvider
{
    private const int TIMEOUT_SECONDS = 5;
    private readonly JsonSerializerOptions camelCaseSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    private readonly HttpClient client = new() { BaseAddress = new Uri(apiAddress) };
    private readonly Mapper mapper = AutoMapperConfiguration.Mapper;

    public async IAsyncEnumerable<Result<IEnumerable<Asset>>> GetAssets()
    {
        var authorization = await client.Authorize(username, password);
        if (!authorization.TryGet(out var authorizedClient))
        {
            yield return Result.FromException<IEnumerable<Asset>>(authorization.Error!);
            yield break;
        }

        var assetsPage = 1;
        bool ok;
        do
        {
            var response = await authorizedClient.GetAsync(
                $"/api/instruments/v1/instruments?page={assetsPage}"
            );
            if (!response.IsSuccessStatusCode)
            {
                yield return Result.FromException<IEnumerable<Asset>>(
                    new Exception($"Failed to get assets: {response.ReasonPhrase}")
                );
                break;
            }

            var assets = await response.Content.ReadFromJsonAsync<InstrumentsResponse>(
                camelCaseSerializerOptions
            );
            yield return Result.FromValue(mapper.Map<IEnumerable<Asset>>(assets.Data));

            ok = assetsPage++ != assets.Paging.Pages;
        } while (ok);
    }

    public async Task<Result<IEnumerable<OhlcBar>>> GetAssetHistoricalPrices(
        Asset asset,
        Provider provider,
        int Interval,
        DateInterval periodicity,
        int Count
    )
    {
        var authorization = await client.Authorize(username, password);
        if (!authorization.TryGet(out var authorizedClient))
        {
            return Result.FromException<IEnumerable<OhlcBar>>(authorization.Error!);
        }

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(
                $"/api/bars/v1/bars/count-back?instrumentId={asset.Id}&provider={provider.Name}&interval={Interval}&periodicity={periodicity}&barsCount={Count}",
                UriKind.Relative
            ),
        };
        return await GetCountBackAsync(authorizedClient, request);
    }

    private async Task<Result<IEnumerable<OhlcBar>>> GetCountBackAsync(
        HttpClient client,
        HttpRequestMessage request
    )
    {
        var response = await client.SendAsyncWithTimeout(
            request,
            TimeSpan.FromSeconds(TIMEOUT_SECONDS)
        );
        if (!response.TryGet(out var responseValue))
        {
            return Result.FromException<IEnumerable<OhlcBar>>(
                new Exception("Timed out trying to get historical prices", response.Error!)
            );
        }
        if (!responseValue.IsSuccessStatusCode)
        {
            return Result.FromException<IEnumerable<OhlcBar>>(
                new Exception("Failed to get historical prices")
            );
        }

        return Result.FromValue(
            mapper.Map<IEnumerable<OhlcBar>>(
                await responseValue.Content.ReadFromJsonAsync<CountBack>(camelCaseSerializerOptions)
            )
        );
    }

    public async IAsyncEnumerable<Result<AssetRealtimePrice>> GetAssetRealtimePrices(
        Asset asset,
        Provider provider,
        IEnumerable<RealtimePriceKind> kinds,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var tokenResult = await client.GetApiKey(username, password);
        if (!tokenResult.TryGet(out var token))
        {
            yield return Result.FromException<AssetRealtimePrice>(tokenResult.Error!);
            yield break;
        }
        using ClientWebSocket webSocket = await ConnectWebSocket(
            token,
            asset,
            provider,
            kinds,
            cancellationToken
        );

        while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var buffer = MemoryPool<byte>.Shared.Rent(1024);
            var result = await webSocket.ReceiveAsync(buffer.Memory, cancellationToken);

            await foreach (
                var realtimePrice in ExtractFromJson(
                    asset,
                    kinds,
                    buffer.Memory.Slice(0, result.Count)
                )
            )
            {
                yield return Result.FromValue(realtimePrice);
            }
        }
        if (cancellationToken.IsCancellationRequested)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<AssetRealtimePrice> ExtractFromJson(
        Asset asset,
        IEnumerable<RealtimePriceKind> kinds,
        Memory<byte> buffer
    )
    {
        var message = JsonNode.Parse(buffer.Span)!;
        foreach (var child in message.AsObject())
        {
            if (
                kinds.Any(k =>
                    k.ToString().Equals(child.Key, StringComparison.InvariantCultureIgnoreCase)
                )
            )
            {
                var childObject = child.Value!;
                var assetRealtimePrice = new AssetRealtimePrice(
                    asset,
                    Enum.Parse<RealtimePriceKind>(child.Key, true),
                    (double)childObject["price"]!,
                    (int)childObject["volume"]!,
                    DateTime.Parse((string)childObject["timestamp"]!)
                );

                yield return assetRealtimePrice;
            }
        }
    }

    private static async Task<ClientWebSocket> ConnectWebSocket(
        string token,
        Asset asset,
        Provider provider,
        IEnumerable<RealtimePriceKind> kinds,
        CancellationToken cancellationToken
    )
    {
        var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(
            new Uri(
                $"wss://platform.fintacharts.com/api/streaming/ws/v1/realtime?token={token}",
                UriKind.Absolute
            ),
            cancellationToken
        );
        var array = new JsonArray();
        array.AddAll(kinds.Select(kind => JsonValue.Create(kind.ToString())));
        var json = new JsonObject
        {
            ["type"] = "l1-subscription",
            ["id"] = "1",
            ["instrumentId"] = asset.Id,
            ["provider"] = provider.Name,
            ["subscribe"] = true,
            ["kinds"] = array,
        };
        await webSocket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(json.ToJsonString())),
            WebSocketMessageType.Text,
            true,
            cancellationToken
        );
        return webSocket;
    }
}
