using DailyWork.Agents.Conversations;
using System.Text.Json;

namespace DailyWork.Agents.Test.Conversations;

public class ConversationMetadataEntityTests
{
    [Fact]
    public void Serialization_RoundTrip_PreservesAllProperties()
    {
        ConversationMetadataEntity entity = CreateEntity();

        string json = JsonSerializer.Serialize(entity);
        ConversationMetadataEntity? deserialized = JsonSerializer.Deserialize<ConversationMetadataEntity>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(entity.Id, deserialized.Id);
        Assert.Equal(entity.Title, deserialized.Title);
        Assert.Equal(entity.CreatedAt, deserialized.CreatedAt);
        Assert.Equal(entity.LastMessageAt, deserialized.LastMessageAt);
        Assert.Equal(entity.MessageCount, deserialized.MessageCount);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("title")]
    [InlineData("createdAt")]
    [InlineData("lastMessageAt")]
    [InlineData("messageCount")]
    public void Serialization_UsesJsonPropertyNames(string propertyName)
    {
        ConversationMetadataEntity entity = CreateEntity();
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(entity));

        Assert.True(document.RootElement.TryGetProperty(propertyName, out _));
    }

    private static ConversationMetadataEntity CreateEntity() => new()
    {
        Id = "conversation-123",
        Title = "Daily planning",
        CreatedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        LastMessageAt = new DateTime(2025, 1, 2, 4, 5, 6, DateTimeKind.Utc),
        MessageCount = 7
    };
}
