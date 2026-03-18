using System.ComponentModel;
using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Entities;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Knowledge.Tools;

[McpServerToolType]
public class NoteTools(KnowledgeDbContext db)
{
    [McpServerTool, Description("Save a free-form note with markdown support to the knowledge base")]
    public async Task<object> SaveNote(
        string title,
        string content,
        string? description = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        KnowledgeItem item = new()
        {
            Id = Guid.NewGuid(),
            Type = KnowledgeItemType.Note,
            Title = title,
            Content = content,
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
            item.Description,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.CreatedAt
        };
    }

    [McpServerTool, Description("Get a saved note by its ID")]
    public async Task<object> GetNote(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Note, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return new { Error = "Note not found" };
        }

        return new
        {
            item.Id,
            item.Type,
            item.Title,
            item.Content,
            item.Description,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.CreatedAt,
            item.UpdatedAt
        };
    }

    [McpServerTool, Description("Update a saved note's fields (title, content, description)")]
    public async Task<object> UpdateNote(
        string id,
        string? title = null,
        string? content = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Note, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return new { Error = "Note not found" };
        }

        if (title is not null)
        {
            item.Title = title;
        }

        if (content is not null)
        {
            item.Content = content;
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
            item.Description,
            Tags = item.Tags.Select(t => t.Name).ToArray(),
            item.UpdatedAt
        };
    }

    [McpServerTool, Description("Delete a saved note by its ID")]
    public async Task<object> DeleteNote(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return new { Error = "Invalid ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .FirstOrDefaultAsync(i => i.Id == guid && i.Type == KnowledgeItemType.Note, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return new { Error = "Note not found" };
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
