using Microsoft.Extensions.Configuration;

namespace DailyWork.Cli.Test;

public class DailyWorkApiOptionsTests
{
    [Fact]
    public void BaseAddress_Default_IsLocalhost()
    {
        DailyWorkApiOptions sut = new();

        Assert.Equal("https://localhost:7048", sut.BaseAddress);
    }

    [Fact]
    public void ChatEndpoint_Default_IsApiChat()
    {
        DailyWorkApiOptions sut = new();

        Assert.Equal("/api/chat", sut.ChatEndpoint);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        DailyWorkApiOptions sut = new()
        {
            BaseAddress = "https://example.test",
            ChatEndpoint = "/custom/chat",
        };

        Assert.Equal("https://example.test", sut.BaseAddress);
        Assert.Equal("/custom/chat", sut.ChatEndpoint);
    }

    [Fact]
    public void BaseAddress_CanBindFromConfiguration()
    {
        Dictionary<string, string?> values = new()
        {
            [$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.BaseAddress)}"] = "https://contoso.test",
            [$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.ChatEndpoint)}"] = "/chat/live",
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        DailyWorkApiOptions sut = new();

        configuration.GetSection(nameof(DailyWorkApiOptions)).Bind(sut);

        Assert.Equal("https://contoso.test", sut.BaseAddress);
        Assert.Equal("/chat/live", sut.ChatEndpoint);
    }
}
