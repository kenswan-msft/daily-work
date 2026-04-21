using System.Net;
using System.Net.Http.Json;
using NSubstitute;

namespace DailyWork.Cli.Test;

public class ApiSettingsClientTests
{
    private readonly MockHttpMessageHandler mockHandler = new();

    private ApiSettingsClient CreateSut()
    {
        HttpClient httpClient = new(mockHandler) { BaseAddress = new Uri("https://localhost:7048") };

        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("DailyWorkApi").Returns(httpClient);

        return new ApiSettingsClient(factory);
    }

    [Fact]
    public async Task GetModelDeploymentAsync_SuccessfulResponse_ReturnsDeploymentName()
    {
        mockHandler.SetResponse(
            "/api/settings",
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { modelDeployment = "docker.io/gemma4:26B" }),
            });

        ApiSettingsClient sut = CreateSut();

        string? result = await sut.GetModelDeploymentAsync(TestContext.Current.CancellationToken);

        Assert.Equal("docker.io/gemma4:26B", result);
    }

    [Fact]
    public async Task GetModelDeploymentAsync_ServerError_ReturnsNull()
    {
        mockHandler.SetResponse(
            "/api/settings",
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        ApiSettingsClient sut = CreateSut();

        string? result = await sut.GetModelDeploymentAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetModelDeploymentAsync_ConnectionRefused_ReturnsNull()
    {
        HttpClient httpClient = new(new ThrowingHttpMessageHandler())
        {
            BaseAddress = new Uri("https://localhost:7048"),
        };

        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("DailyWorkApi").Returns(httpClient);

        ApiSettingsClient sut = new(factory);

        string? result = await sut.GetModelDeploymentAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetModelDeploymentAsync_NullDeployment_ReturnsNull()
    {
        mockHandler.SetResponse(
            "/api/settings",
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { modelDeployment = (string?)null }),
            });

        ApiSettingsClient sut = CreateSut();

        string? result = await sut.GetModelDeploymentAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }
}
