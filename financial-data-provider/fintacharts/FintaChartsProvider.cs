using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using app_core.domain;
using app_core.service;
using AutoMapper;
using DotNext;

namespace financial_data_provider.fintacharts;

public class FintaChartsProvider(string apiAddress, string username, string password)
    : IAssetProvider
{
    private readonly JsonSerializerOptions snakeCaseSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, WriteIndented = true };
    private readonly JsonSerializerOptions camelCaseSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    private readonly HttpClient client = new() { BaseAddress = new Uri(apiAddress) };
    private readonly Mapper mapper =
        new(new MapperConfiguration(cfg => cfg.CreateMap<Instrument, Asset>()));

    private async Task<Result<string>> GetApiKey()
    {
        var response = await client.PostAsync(
            "/identity/realms/fintatech/protocol/openid-connect/token",
            new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("client_id", "app-cli"),
                ]
            )
        );

        if (!response.IsSuccessStatusCode)
            return Result.FromException<string>(
                new Exception($"Failed to get API key: {response.ReasonPhrase}")
            );

        return (
                await response.Content.ReadFromJsonAsync<ApiKeyResponse>(snakeCaseSerializerOptions)
            ).AccessToken ?? Result.FromException<string>(new Exception("Couldn't parse API key"));
    }

    public async IAsyncEnumerable<Result<IEnumerable<Asset>>> GetAssets()
    {
        var authorization = await SetUpAuthorization();
        if (!authorization.TryGet(out var authorizedClient))
        {
            yield return authorization.Convert<IEnumerable<Asset>>((_) => []);
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
                yield return Result.FromException<IEnumerable<Asset>>(
                    new Exception("Failed to get assets")
                );
            }

            var assets = await response.Content.ReadFromJsonAsync<InstrumentsResponse>(
                camelCaseSerializerOptions
            );

            yield return Result.FromValue(mapper.Map<IEnumerable<Asset>>(assets.Data));
            ok = assetsPage++ != assets.Paging.Pages;
        } while (ok);
    }

    private async Task<Result<HttpClient>> SetUpAuthorization()
    {
        var apiKey = await GetApiKey();
        if (!apiKey.IsSuccessful)
        {
            return Result.FromException<HttpClient>(
                new Exception("Failed to get API key", apiKey.Error)
            );
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey.Value!
        );

        return Result.FromValue(client);
    }
}
