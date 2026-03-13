using System.Net;

namespace DailyWork.Api.Test;

public class HealthEndpointTests(DailyWorkApiFactory factory) : IClassFixture<DailyWorkApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task GetAsync_HealthEndpoint_ReturnsSuccessStatusCode(string path)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
