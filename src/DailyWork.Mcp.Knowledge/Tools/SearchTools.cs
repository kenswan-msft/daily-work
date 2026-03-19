using System.ComponentModel;
using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Knowledge.Tools;

[McpServerToolType]
public class SearchTools(KnowledgeDbContext db, ILogger<SearchTools> logger)
{
    [McpServerTool, Description("Search across all knowledge items (links, snippets, notes) by keyword. Searches title, description, content, URL, and tag names.")]
    public async Task<object[]> Search(
        string query,
        string? type = null,
        string? tag = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching knowledge base for '{Query}'", query);

        IQueryable<KnowledgeItem> items = db.KnowledgeItems
            .Include(i => i.Tags)
            .AsQueryable();

        if (type is not null && Enum.TryParse<KnowledgeItemType>(type, ignoreCase: true, out KnowledgeItemType itemType))
        {
            items = items.Where(i => i.Type == itemType);
        }

        if (tag is not null)
        {
            items = items.Where(i => i.Tags.Any(t => t.Name == tag));
        }

        string searchTerm = $"%{query}%";
        items = items.Where(i =>
            EF.Functions.Like(i.Title, searchTerm) ||
            (i.Description != null && EF.Functions.Like(i.Description, searchTerm)) ||
            (i.Content != null && EF.Functions.Like(i.Content, searchTerm)) ||
            (i.Url != null && EF.Functions.Like(i.Url, searchTerm)) ||
            i.Tags.Any(t => EF.Functions.Like(t.Name, searchTerm)));

        List<KnowledgeItem> results = await items
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results.Select(i => (object)new
        {
            i.Id,
            Type = i.Type.ToString(),
            i.Title,
            i.Description,
            i.Url,
            ContentPreview = i.Content != null && i.Content.Length > 200
                ? i.Content[..200] + "..."
                : i.Content,
            i.Language,
            i.Category,
            Tags = i.Tags.Select(t => t.Name).ToArray(),
            i.CreatedAt
        }).ToArray();
    }

    [McpServerTool, Description("List knowledge items filtered by tag name, optionally filtered by type")]
    public async Task<object[]> ListByTag(
        string tagName,
        string? type = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing items by tag '{TagName}'", tagName);

        IQueryable<KnowledgeItem> items = db.KnowledgeItems
            .Include(i => i.Tags)
            .Where(i => i.Tags.Any(t => t.Name == tagName));

        if (type is not null && Enum.TryParse<KnowledgeItemType>(type, ignoreCase: true, out KnowledgeItemType itemType))
        {
            items = items.Where(i => i.Type == itemType);
        }

        List<KnowledgeItem> results = await items
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results.Select(i => (object)new
        {
            i.Id,
            Type = i.Type.ToString(),
            i.Title,
            i.Description,
            i.Url,
            ContentPreview = i.Content != null && i.Content.Length > 200
                ? i.Content[..200] + "..."
                : i.Content,
            i.Language,
            i.Category,
            Tags = i.Tags.Select(t => t.Name).ToArray(),
            i.CreatedAt
        }).ToArray();
    }

    [McpServerTool, Description("List the most recently saved knowledge items, optionally filtered by type")]
    public async Task<object[]> ListRecent(
        string? type = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing {Count} recent knowledge items", limit);

        IQueryable<KnowledgeItem> items = db.KnowledgeItems
            .Include(i => i.Tags)
            .AsQueryable();

        if (type is not null && Enum.TryParse<KnowledgeItemType>(type, ignoreCase: true, out KnowledgeItemType itemType))
        {
            items = items.Where(i => i.Type == itemType);
        }

        List<KnowledgeItem> results = await items
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results.Select(i => (object)new
        {
            i.Id,
            Type = i.Type.ToString(),
            i.Title,
            i.Description,
            i.Url,
            ContentPreview = i.Content != null && i.Content.Length > 200
                ? i.Content[..200] + "..."
                : i.Content,
            i.Language,
            i.Category,
            Tags = i.Tags.Select(t => t.Name).ToArray(),
            i.CreatedAt
        }).ToArray();
    }
}
