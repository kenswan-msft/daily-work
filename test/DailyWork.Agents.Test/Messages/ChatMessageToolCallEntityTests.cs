using System.Text.Json;
using DailyWork.Agents.Messages;

namespace DailyWork.Agents.Test.Messages;

public class ChatMessageToolCallEntityTests
{
    [Fact]
    public void Serialization_RoundTrip_PreservesAllProperties()
    {
        ChatMessageToolCallEntity entity = CreateEntity();

        string json = JsonSerializer.Serialize(entity);
        ChatMessageToolCallEntity? deserialized = JsonSerializer.Deserialize<ChatMessageToolCallEntity>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(entity.Id, deserialized.Id);
        Assert.Equal(entity.ConversationId, deserialized.ConversationId);
        Assert.Equal(entity.ToolName, deserialized.ToolName);
        Assert.Equal(entity.Arguments, deserialized.Arguments);
        Assert.Equal(entity.Result, deserialized.Result);
        Assert.Equal(entity.IsError, deserialized.IsError);
        Assert.Equal(entity.Timestamp, deserialized.Timestamp);
    }

    [Fact]
    public void Serialization_NullableFields_WhenNull_RoundTripsCorrectly()
    {
        ChatMessageToolCallEntity entity = CreateEntity() with
        {
            Arguments = null,
            Result = null
        };

        string json = JsonSerializer.Serialize(entity);
        ChatMessageToolCallEntity? deserialized = JsonSerializer.Deserialize<ChatMessageToolCallEntity>(json);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Arguments);
        Assert.Null(deserialized.Result);
    }

    [Fact]
    public void IsError_WhenTrue_RoundTripsCorrectly()
    {
        ChatMessageToolCallEntity entity = CreateEntity() with
        {
            IsError = true,
            Result = "Something went wrong"
        };

        string json = JsonSerializer.Serialize(entity);
        ChatMessageToolCallEntity? deserialized = JsonSerializer.Deserialize<ChatMessageToolCallEntity>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.IsError);
        Assert.Equal("Something went wrong", deserialized.Result);
    }

    private static ChatMessageToolCallEntity CreateEntity() => new()
    {
        Id = "toolcall-123",
        ConversationId = "conversation-456",
        ToolName = "get_weather",
        Arguments = """{"city":"Seattle"}""",
        Result = """{"temp":72}""",
        IsError = false,
        Timestamp = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
    };
}
