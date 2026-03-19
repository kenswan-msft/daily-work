using DailyWork.Agents.Conversations;
using DailyWork.Agents.Data;
using DailyWork.Agents.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DailyWork.Agents.Test.Conversations;

public class ConversationServiceTests
{
    [Fact]
    public async Task GetConversationsAsync_ReturnsConversationsFromDatabase()
    {
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);

        ConversationMetadataEntity[] expected =
        [
            CreateMetadataEntity("conversation-2", "Second", 5,
                lastMessageAt: new DateTime(2025, 1, 2, 5, 0, 0, DateTimeKind.Utc)),
            CreateMetadataEntity("conversation-1", "First", 2,
                lastMessageAt: new DateTime(2025, 1, 2, 4, 5, 6, DateTimeKind.Utc))
        ];

        using (ConversationsDbContext seedContext = dbContextFactory.CreateDbContext())
        {
            seedContext.ConversationMetadata.AddRange(expected);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        ConversationService sut = CreateService(dbContextFactory);

        IReadOnlyList<ConversationMetadataEntity> conversations =
            await sut.GetConversationsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, conversations.Count);
        Assert.Equal("conversation-2", conversations[0].Id);
        Assert.Equal("conversation-1", conversations[1].Id);
    }

    [Fact]
    public async Task GetConversationsAsync_ReturnsEmpty_WhenNoResults()
    {
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ConversationService sut = CreateService(dbContextFactory);

        IReadOnlyList<ConversationMetadataEntity> conversations =
            await sut.GetConversationsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(conversations);
    }

    [Fact]
    public async Task GetConversationMessagesAsync_ReturnsMessagesForConversation()
    {
        const string conversationId = "conversation-123";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);

        var entity = new ChatMessageEntity
        {
            Id = "message-1",
            ConversationId = conversationId,
            Role = "assistant",
            Content = "fallback content",
            Timestamp = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            SerializedMessage = System.Text.Json.JsonSerializer.Serialize(
                new Microsoft.Extensions.AI.ChatMessage(
                    Microsoft.Extensions.AI.ChatRole.Assistant, "Hello from serialized message"))
        };

        using (ConversationsDbContext seedContext = dbContextFactory.CreateDbContext())
        {
            seedContext.ChatMessages.Add(entity);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        ConversationService sut = CreateService(dbContextFactory);

        IReadOnlyList<ConversationMessageSummary> messages =
            await sut.GetConversationMessagesAsync(conversationId, TestContext.Current.CancellationToken);

        ConversationMessageSummary message = Assert.Single(messages);
        Assert.Equal(entity.Id, message.Id);
        Assert.Equal("assistant", message.Role);
        Assert.Equal("Hello from serialized message", message.Content);
        Assert.Equal(entity.Timestamp, message.Timestamp);
    }

    [Fact]
    public async Task CreateOrUpdateMetadataAsync_CreatesNewMetadata_WhenConversationDoesNotExist()
    {
        const string conversationId = "conversation-123";
        const string title = "New conversation";
        const int newMessageCount = 3;
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ConversationService sut = CreateService(dbContextFactory);

        DateTime before = DateTime.UtcNow;

        await sut.CreateOrUpdateMetadataAsync(
            conversationId,
            title,
            newMessageCount,
            TestContext.Current.CancellationToken);

        DateTime after = DateTime.UtcNow;

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        ConversationMetadataEntity? createdMetadata =
            await verifyContext.ConversationMetadata.FindAsync([conversationId], TestContext.Current.CancellationToken);

        Assert.NotNull(createdMetadata);
        Assert.Equal(conversationId, createdMetadata.Id);
        Assert.Equal(title, createdMetadata.Title);
        Assert.Equal(newMessageCount, createdMetadata.MessageCount);
        Assert.Equal(createdMetadata.CreatedAt, createdMetadata.LastMessageAt);
        Assert.InRange(createdMetadata.CreatedAt, before, after);
    }

    [Fact]
    public async Task CreateOrUpdateMetadataAsync_UpdatesExistingMetadata_WhenConversationExists()
    {
        const string conversationId = "conversation-123";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);

        ConversationMetadataEntity existingMetadata = CreateMetadataEntity(conversationId, "Original title", 4);
        DateTime originalLastMessageAt = existingMetadata.LastMessageAt;

        using (ConversationsDbContext seedContext = dbContextFactory.CreateDbContext())
        {
            seedContext.ConversationMetadata.Add(existingMetadata);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        ConversationService sut = CreateService(dbContextFactory);
        DateTime before = DateTime.UtcNow;

        await sut.CreateOrUpdateMetadataAsync(
            conversationId,
            "Ignored new title",
            2,
            TestContext.Current.CancellationToken);

        DateTime after = DateTime.UtcNow;

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        ConversationMetadataEntity? replacedMetadata =
            await verifyContext.ConversationMetadata.FindAsync([conversationId], TestContext.Current.CancellationToken);

        Assert.NotNull(replacedMetadata);
        Assert.Equal(conversationId, replacedMetadata.Id);
        Assert.Equal("Original title", replacedMetadata.Title);
        Assert.Equal(6, replacedMetadata.MessageCount);
        Assert.InRange(replacedMetadata.LastMessageAt, before, after);
        Assert.NotEqual(originalLastMessageAt, replacedMetadata.LastMessageAt);
    }

    [Fact]
    public async Task UpdateTitleAsync_UpdatesExistingTitle()
    {
        const string conversationId = "conversation-123";
        const string title = "Updated title";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);

        using (ConversationsDbContext seedContext = dbContextFactory.CreateDbContext())
        {
            seedContext.ConversationMetadata.Add(CreateMetadataEntity(conversationId, "Original title", 4));
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        ConversationService sut = CreateService(dbContextFactory);

        await sut.UpdateTitleAsync(conversationId, title, TestContext.Current.CancellationToken);

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        ConversationMetadataEntity? updatedMetadata =
            await verifyContext.ConversationMetadata.FindAsync([conversationId], TestContext.Current.CancellationToken);

        Assert.NotNull(updatedMetadata);
        Assert.Equal(title, updatedMetadata.Title);
    }

    [Fact]
    public async Task UpdateTitleAsync_HandlesNotFoundGracefully()
    {
        const string conversationId = "conversation-123";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ConversationService sut = CreateService(dbContextFactory);

        await sut.UpdateTitleAsync(
            conversationId,
            "Updated title",
            TestContext.Current.CancellationToken);

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        Assert.Empty(verifyContext.ConversationMetadata);
    }

    private static IDbContextFactory<ConversationsDbContext> CreateDbContextFactory(string dbName)
    {
        DbContextOptions<ConversationsDbContext> options = new DbContextOptionsBuilder<ConversationsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        IDbContextFactory<ConversationsDbContext> factory =
            Substitute.For<IDbContextFactory<ConversationsDbContext>>();

        factory.CreateDbContext()
            .Returns(_ => new ConversationsDbContext(options));

        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new ConversationsDbContext(options)));

        return factory;
    }

    private static ConversationService CreateService(IDbContextFactory<ConversationsDbContext> dbContextFactory) =>
        new(dbContextFactory, Substitute.For<ILogger<ConversationService>>());

    private static ConversationMetadataEntity CreateMetadataEntity(
        string id,
        string title,
        int messageCount,
        DateTime? lastMessageAt = null) => new()
        {
            Id = id,
            Title = title,
            CreatedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            LastMessageAt = lastMessageAt ?? new DateTime(2025, 1, 2, 4, 5, 6, DateTimeKind.Utc),
            MessageCount = messageCount
        };
}
