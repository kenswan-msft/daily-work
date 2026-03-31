using System.Net;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Cli.Test;

public class ApiHealthCheckerTests
{
    private readonly MockHttpMessageHandler mockHandler = new();

    private ApiHealthChecker CreateSut(string baseAddress = "https://localhost:7048")
    {
        HttpClient httpClient = new(mockHandler) { BaseAddress = new Uri(baseAddress) };

        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("DailyWorkApi").Returns(httpClient);

        IOptions<DailyWorkApiOptions> options = Options.Create(new DailyWorkApiOptions
        {
            BaseAddress = baseAddress,
        });

        return new ApiHealthChecker(factory, options);
    }

    [Fact]
    public async Task IsApiReachableAsync_HealthyResponse_ReturnsTrue()
    {
        mockHandler.SetResponse("/health", new HttpResponseMessage(HttpStatusCode.OK));

        ApiHealthChecker sut = CreateSut();

        bool result = await sut.IsApiReachableAsync(TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsApiReachableAsync_ServerError_ReturnsFalse()
    {
        mockHandler.SetResponse(
            "/health",
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        ApiHealthChecker sut = CreateSut();

        bool result = await sut.IsApiReachableAsync(TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task IsApiReachableAsync_ConnectionRefused_ReturnsFalse()
    {
        // Use a handler that throws to simulate connection refused
        ThrowingHttpMessageHandler throwingHandler = new();
        HttpClient httpClient = new(throwingHandler) { BaseAddress = new Uri("https://localhost:7048") };

        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("DailyWorkApi").Returns(httpClient);

        IOptions<DailyWorkApiOptions> options = Options.Create(new DailyWorkApiOptions());

        ApiHealthChecker sut = new(factory, options);

        bool result = await sut.IsApiReachableAsync(TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }
}
