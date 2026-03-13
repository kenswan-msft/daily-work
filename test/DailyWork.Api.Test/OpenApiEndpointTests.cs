using System.Text.Json;

namespace DailyWork.Api.Test;

public class OpenApiEndpointTests(DailyWorkApiFactory factory) : IClassFixture<DailyWorkApiFactory>
{
    [Fact]
    public async Task GetAsync_OpenApiDocument_ReturnsValidJson()
    {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response =
            await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);

        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.True(document.RootElement.TryGetProperty("openapi", out JsonElement version));
        Assert.False(string.IsNullOrWhiteSpace(version.GetString()));
    }
}
