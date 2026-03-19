using System.ComponentModel;
using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Knowledge.Tools;

[McpServerToolType]
public class TagTools(KnowledgeDbContext db, ILogger<TagTools> logger)
{
    [McpServerTool, Description("List all knowledge tags with counts of how many items use each tag")]
    public async Task<object[]> ListTags(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing knowledge tags");

        List<KnowledgeTag> tags = await db.KnowledgeTags
            .Include(t => t.Items)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tags.Select(t => (object)new
        {
            t.Id,
            t.Name,
            ItemCount = t.Items.Count,
            LinkCount = t.Items.Count(i => i.Type == KnowledgeItemType.Link),
            SnippetCount = t.Items.Count(i => i.Type == KnowledgeItemType.Snippet),
            NoteCount = t.Items.Count(i => i.Type == KnowledgeItemType.Note)
        }).ToArray();
    }

    [McpServerTool, Description("Add or remove a tag from a knowledge item. Action can be 'add' or 'remove'. Tags are auto-created if they don't exist.")]
    public async Task<object> TagItem(
        string itemId,
        string tagName,
        string action = "add",
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Tagging item {ItemId} with '{TagName}' (action: {Action})", itemId, tagName, action);

        if (!Guid.TryParse(itemId, out Guid guid))
        {
            return new { Error = "Invalid item ID format" };
        }

        KnowledgeItem? item = await db.KnowledgeItems
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == guid, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            logger.LogWarning("Knowledge item {ItemId} not found", itemId);
            return new { Error = "Knowledge item not found" };
        }

        if (action.Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            if (item.Tags.Any(t => t.Name == tagName))
            {
                return new { Message = $"Item already has tag '{tagName}'", item.Id, item.Title };
            }

            KnowledgeTag tag = await FindOrCreateTagAsync(tagName, cancellationToken)
                .ConfigureAwait(false);
            item.Tags.Add(tag);
            item.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new
            {
                Message = $"Tag '{tagName}' added to '{item.Title}'",
                item.Id,
                item.Title,
                Tags = item.Tags.Select(t => t.Name).ToArray()
            };
        }

        if (action.Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            KnowledgeTag? tagToRemove = item.Tags.FirstOrDefault(t => t.Name == tagName);

            if (tagToRemove is null)
            {
                return new { Error = $"Item does not have tag '{tagName}'" };
            }

            item.Tags.Remove(tagToRemove);
            item.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new
            {
                Message = $"Tag '{tagName}' removed from '{item.Title}'",
                item.Id,
                item.Title,
                Tags = item.Tags.Select(t => t.Name).ToArray()
            };
        }

        return new { Error = $"Unknown action '{action}'. Use 'add' or 'remove'." };
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
