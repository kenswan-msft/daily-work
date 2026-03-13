using System.Text.Json;
using DailyWork.Agents.Messages;

namespace DailyWork.Agents.Test.Messages;

public class ChatMessageEntityTests
{
    [Fact]
    public void PartitionKey_ReturnsConversationId()
    {
        ChatMessageEntity entity = CreateEntity();

        Assert.Equal(entity.ConversationId, entity.PartitionKey);
    }

    [Fact]
    public void Serialization_RoundTrip_PreservesAllProperties()
    {
        ChatMessageEntity entity = CreateEntity();

        string json = JsonSerializer.Serialize(entity);
        ChatMessageEntity? deserialized = JsonSerializer.Deserialize<ChatMessageEntity>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(entity.Id, deserialized.Id);
        Assert.Equal(entity.ConversationId, deserialized.ConversationId);
        Assert.Equal(entity.Role, deserialized.Role);
        Assert.Equal(entity.Content, deserialized.Content);
        Assert.Equal(entity.Timestamp, deserialized.Timestamp);
        Assert.Equal(entity.SerializedMessage, deserialized.SerializedMessage);
        Assert.Equal(entity.PartitionKey, deserialized.PartitionKey);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("conversationId")]
    [InlineData("role")]
    [InlineData("content")]
    [InlineData("timestamp")]
    [InlineData("serializedMessage")]
    public void Serialization_UsesJsonPropertyNames(string propertyName)
    {
        ChatMessageEntity entity = CreateEntity();
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(entity));

        Assert.True(document.RootElement.TryGetProperty(propertyName, out _));
    }

    [Fact]
    public void SerializedMessage_WhenNull_IsOmittedOrNull()
    {
        ChatMessageEntity entity = CreateEntity() with { SerializedMessage = null };
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(entity));

        bool hasSerializedMessage = document.RootElement.TryGetProperty("serializedMessage", out JsonElement serializedMessage);

        Assert.True(!hasSerializedMessage || serializedMessage.ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public void Serialization_PartitionKey_IncludedInJson()
    {
        ChatMessageEntity entity = CreateEntity();
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(entity));

        Assert.True(document.RootElement.TryGetProperty("partitionKey", out JsonElement partitionKey));
        Assert.Equal(entity.PartitionKey, partitionKey.GetString());
    }

    private static ChatMessageEntity CreateEntity() => new()
    {
        Id = "message-123",
        ConversationId = "conversation-456",
        Role = "user",
        Content = "Hello from a persisted chat message.",
        Timestamp = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        SerializedMessage = "{\"type\":\"chat\"}",
    };
}
