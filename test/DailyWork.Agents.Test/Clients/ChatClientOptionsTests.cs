using System.ComponentModel.DataAnnotations;
using DailyWork.Agents.Clients;

namespace DailyWork.Agents.Test.Clients;

public class ChatClientOptionsTests
{
    [Fact]
    public void Properties_DefaultInstance_HaveExpectedDefaults()
    {
        ChatClientOptions options = new();

        Assert.Equal("ai/gpt-oss", options.Deployment);
        Assert.Equal("http://localhost:12434/engines/v1", options.Endpoint);
        Assert.Equal(ChatClientSource.Docker, options.Source);
    }

    [Fact]
    public void Properties_DefaultValues_PassValidation()
    {
        ChatClientOptions options = new();

        (bool isValid, List<ValidationResult> validationResults) = Validate(options);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Deployment_NullOrEmpty_FailsValidation(string? deployment)
    {
        ChatClientOptions options = new()
        {
            Deployment = deployment!,
        };

        (bool isValid, List<ValidationResult> validationResults) = Validate(options);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(ChatClientOptions.Deployment)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Endpoint_NullOrEmpty_FailsValidation(string? endpoint)
    {
        ChatClientOptions options = new()
        {
            Endpoint = endpoint!,
        };

        (bool isValid, List<ValidationResult> validationResults) = Validate(options);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(ChatClientOptions.Endpoint)));
    }

    [Fact]
    public void ChatClientSource_ExpectedMembers_HaveExpectedValues()
    {
        Assert.Equal(0, (int)ChatClientSource.Copilot);
        Assert.Equal(1, (int)ChatClientSource.Docker);
    }

    private static (bool IsValid, List<ValidationResult> Results) Validate(ChatClientOptions options)
    {
        List<ValidationResult> validationResults = [];
        ValidationContext validationContext = new(options);
        bool isValid = Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true);

        return (isValid, validationResults);
    }
}
