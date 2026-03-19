using System.ComponentModel;
using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Knowledge.Tools;

[McpServerToolType]
public class LinkTools(KnowledgeDbContext db, ILogger<LinkTools> logger)
{
    [McpServerTool, Description("Save a URL/link with title, description, category, and tags to the knowledge base")]
    public async Task<object> SaveLink(
        string url,
        string title,
        string? description = null,
        string? category = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving link '{Title}' ({Url})", title, url);

        KnowledgeItem item = new()
        {
            Id = Guid.NewGuid(),
            Type = KnowledgeItemType.Link,
            Title = title,
            Url = url,
            Description = description,
            Category = category
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
            item.Url,
            item.Description,
            item.Category,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.CreatedAt
        };
    }

    [McpServerTool, Description("Get a saved link by its ID")]
    public async Task<object> GetLink(
        string id,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving link {LinkId}", id);

        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Link, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            logger.LogWarning("Link {LinkId} not found", id);
            return new { Error = "Link not found" };
        }

        return new
        {
            item.Id,
            item.Type,
            item.Title,
            item.Url,
            item.Description,
            item.Category,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.CreatedAt,
            item.UpdatedAt
        };
    }

    [McpServerTool, Description("Update a saved link's fields (title, url, description, category)")]
    public async Task<object> UpdateLink(
        string id,
        string? title = null,
        string? url = null,
        string? description = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating link {LinkId}", id);

        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Link, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            logger.LogWarning("Link {LinkId} not found", id);
            return new { Error = "Link not found" };
        }

        if (title is not null)
        {
            item.Title = title;
        }

        if (url is not null)
        {
            item.Url = url;
        }

        if (description is not null)
        {
            item.Description = description;
        }

        if (category is not null)
        {
            item.Category = category;
        }

        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            item.Id,
            item.Type,
            item.Title,
            item.Url,
            item.Description,
            item.Category,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.UpdatedAt
        };
    }

    [McpServerTool, Description("Delete a saved link by its ID")]
    public async Task<object> DeleteLink(
        string id,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting link {LinkId}", id);

        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Link, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            logger.LogWarning("Link {LinkId} not found", id);
            return new { Error = "Link not found" };
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
