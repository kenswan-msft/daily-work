using System.ComponentModel;
using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Knowledge.Tools;

[McpServerToolType]
public class SnippetTools(KnowledgeDbContext db, ILogger<SnippetTools> logger)
{
    [McpServerTool, Description("Save a code snippet with title, content, language, description, and tags to the knowledge base")]
    public async Task<object> SaveSnippet(
        string title,
        string content,
        string? language = null,
        string? description = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving snippet '{Title}' in {Language}", title, language);

        KnowledgeItem item = new()
        {
            Id = Guid.NewGuid(),
            Type = KnowledgeItemType.Snippet,
            Title = title,
            Content = content,
            Language = language,
            Description = description
        };

        if (tags is { Length: > 0 })
        {
            foreach (string tagName in tags)
            {
                KnowledgeTag tag = await FindOrCreateTagAsync(tagName.Trim(), cancellationToken)
                    .ConfigureAwait(false);
                item.Tags.Add(tag);
            }
        }

        db.KnowledgeItems.Add(item);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            item.Id,
            item.Type,
            item.Title,
            item.Content,
            item.Language,
            item.Description,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.CreatedAt
        };
    }

    [McpServerTool, Description("Get a saved code snippet by its ID")]
    public async Task<object> GetSnippet(
        string id,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving snippet {SnippetId}", id);

        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Snippet, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            logger.LogWarning("Snippet {SnippetId} not found", id);
            return new { Error = "Snippet not found" };
        }

        return new
        {
            item.Id,
            item.Type,
            item.Title,
            item.Content,
            item.Language,
            item.Description,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.CreatedAt,
            item.UpdatedAt
        };
    }

    [McpServerTool, Description("Update a saved code snippet's fields (title, content, language, description)")]
    public async Task<object> UpdateSnippet(
        string id,
        string? title = null,
        string? content = null,
        string? language = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating snippet {SnippetId}", id);

        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Snippet, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            logger.LogWarning("Snippet {SnippetId} not found", id);
            return new { Error = "Snippet not found" };
        }

        if (title is not null)
        {
            item.Title = title;
        }

        if (content is not null)
        {
            item.Content = content;
        }

        if (language is not null)
        {
            item.Language = language;
        }

        if (description is not null)
        {
            item.Description = description;
        }

        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            item.Id,
            item.Type,
            item.Title,
            item.Content,
            item.Language,
            item.Description,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.UpdatedAt
        };
    }

    [McpServerTool, Description("Delete a saved code snippet by its ID")]
    public async Task<object> DeleteSnippet(
        string id,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting snippet {SnippetId}", id);

        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Snippet, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            logger.LogWarning("Snippet {SnippetId} not found", id);
            return new { Error = "Snippet not found" };
        }

        db.KnowledgeItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new { Deleted = true, item.Id, item.Title };
    }

    private async Task<KnowledgeTag> FindOrCreateTagAsync(string name, CancellationToken cancellationToken)
    {
        KnowledgeTag? tag = await db.KnowledgeTags
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken)
            .ConfigureAwait(false);

        if (tag is not null)
        {
            return tag;
        }

        tag = new KnowledgeTag { Id = Guid.NewGuid(), Name = name };
        db.KnowledgeTags.Add(tag);

        return tag;
    }
}
