using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using app_core.dto;
using app_core.service;
using AutoMapper;
using DotNext;
using Microsoft.Extensions.Logging;

namespace financial_data_provider.fintacharts;

public class FintaChartsProvider(
    string apiAddress,
    string username,
    string password,
    ILogger<FintaChartsProvider> logger
) : IAssetProvider, IAssetsHistoricalPriceProvider
{
    private readonly JsonSerializerOptions camelCaseSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    private readonly HttpClient client = new() { BaseAddress = new Uri(apiAddress) };
    private readonly Mapper mapper =
        new(
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Instrument, Asset>()
                    .ForMember(
                        dest => dest.Providers,
                        opt =>
                            opt.MapFrom(src =>
                                src.Mappings.Keys.Select(m => new Provider(m)).ToList<Provider>()
                            )
                    );

                cfg.CreateMap<CountBack, IEnumerable<OhlcBar>>()
                    .ConvertUsing(cb =>
                        cb.Data.Select(b => new OhlcBar(b.O, b.H, b.L, b.C, b.V, b.T))
                    );
            })
        );

    public async IAsyncEnumerable<Result<IEnumerable<Asset>>> GetAssets()
    {
        var authorization = await client.Authorize(username, password);
        if (!authorization.TryGet(out var authorizedClient))
        {
            yield return Result.FromException<IEnumerable<Asset>>(authorization.Error!);
            yield break;
        }

        var ok = true;
        var assetsPage = 1;
        do
        {
            var response = await authorizedClient.GetAsync(
                $"/api/instruments/v1/instruments?page={assetsPage}"
            );
            if (!response.IsSuccessStatusCode)
            {
                ok = false;
                logger.LogError(
                    "Failed to get assets: {ReasonPhrase}",
                    await response.Content.ReadAsStringAsync()
                );
                yield return Result.FromException<IEnumerable<Asset>>(
                    new Exception($"Failed to get assets: {response.ReasonPhrase}")
                );
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
        logger.LogInformation(
            "GetAssetHistoricalPrices({asset}, {provider}, {Interval}, {periodicity}, {Count})",
            asset,
            provider,
            Interval,
            periodicity,
            Count
        );
        var barsResult = authorization.Convert(async client =>
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(
                    $"/api/bars/v1/bars/count-back?instrumentId={asset.Id}&provider={provider.Name}&interval={Interval}&periodicity={periodicity}&barsCount={Count}",
                    UriKind.Relative
                ),
            };

            var response = await client.SendAsyncWithTimeout(request, TimeSpan.FromSeconds(5));
            if (!response.TryGet(out var responseValue))
            {
                logger.LogError("Timed out trying to get historical prices.");
                return Result.FromException<IEnumerable<OhlcBar>>(
                    new Exception("Timed out trying to get historical prices", response.Error!)
                );
            }
            if (!responseValue.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Failed to get historical prices: {ReasonPhrase}",
                    await responseValue.Content.ReadAsStringAsync()
                );
                return Result.FromException<IEnumerable<OhlcBar>>(
                    new Exception("Failed to get historical prices")
                );
            }

            return Result.FromValue(
                mapper.Map<IEnumerable<OhlcBar>>(
                    await responseValue.Content.ReadFromJsonAsync<CountBack>(
                        camelCaseSerializerOptions
                    )
                )
            );
        });

        if (!barsResult.TryGet(out var barsTask))
        {
            return Result.FromException<IEnumerable<OhlcBar>>(barsResult.Error!);
        }

        return await barsTask;
    }
}
