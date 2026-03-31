using Microsoft.Extensions.Options;

namespace DailyWork.Cli;

public sealed class ApiHealthChecker(IHttpClientFactory httpClientFactory, IOptions<DailyWorkApiOptions> apiOptions)
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(3);

    public async Task<bool> IsApiReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient("DailyWorkApi");
            client.Timeout = HealthCheckTimeout;

            HttpResponseMessage response =
                await client.GetAsync(
                    new Uri(new Uri(apiOptions.Value.BaseAddress), "/health"),
                    cancellationToken).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return false;
        }
    }
}
