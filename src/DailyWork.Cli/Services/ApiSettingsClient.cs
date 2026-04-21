using System.Net.Http.Json;

namespace DailyWork.Cli;

public class ApiSettingsClient(IHttpClientFactory httpClientFactory)
{
    public async Task<string?> GetModelDeploymentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient("DailyWorkApi");

            ApiSettingsResponse? response =
                await client.GetFromJsonAsync<ApiSettingsResponse>(
                        "/api/settings", cancellationToken)
                    .ConfigureAwait(false);

            return response?.ModelDeployment;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException
                or OperationCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private sealed record ApiSettingsResponse(string? ModelDeployment);
}
