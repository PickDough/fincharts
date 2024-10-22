using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNext;

namespace financial_data_provider.fintacharts;

internal static class HttpClientExtensions
{
    public static async Task<Result<HttpResponseMessage>> SendAsyncWithTimeout(
        this HttpClient client,
        HttpRequestMessage request,
        TimeSpan timeout
    )
    {
        HttpResponseMessage response;
        try
        {
            using var tokenSource = new CancellationTokenSource(timeout);
            response = await client.SendAsync(request, tokenSource.Token);
            return Result.FromValue(response);
        }
        catch (Exception ex)
        {
            return Result.FromException<HttpResponseMessage>(ex);
        }
    }

    public static async Task<Result<HttpClient>> Authorize(
        this HttpClient client,
        string username,
        string password
    )
    {
        var apiKey = await GetApiKey(client, username, password);
        if (!apiKey.IsSuccessful)
        {
            return Result.FromException<HttpClient>(apiKey.Error);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey.Value!
        );

        return Result.FromValue(client);
    }

    public static async Task<Result<string>> GetApiKey(
        this HttpClient client,
        string username,
        string password
    )
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
        {
            return Result.FromException<string>(new Exception($"Failed to get API key"));
        }

        return (
                await response.Content.ReadFromJsonAsync<ApiKeyResponse>(
                    new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        WriteIndented = true,
                    }
                )
            ).AccessToken ?? Result.FromException<string>(new Exception("Couldn't parse API key"));
    }
}
