using System.Net;
using System.Net.Http.Json;

namespace DailyWork.Api.Test;

public class SettingsEndpointTests(DailyWorkApiFactory factory) : IClassFixture<DailyWorkApiFactory>
{
    [Fact]
    public async Task GetSettings_ReturnsOkWithModelDeployment()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/settings", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettingsResponse? body = await response.Content
            .ReadFromJsonAsync<SettingsResponse>(TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.ModelDeployment));
    }

    private sealed record SettingsResponse(string? ModelDeployment);
}
