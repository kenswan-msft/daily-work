using DailyWork.Api.Dashboard.Models;
using DailyWork.Mcp.Knowledge.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Api.Dashboard;

public static class KnowledgeDashboardEndpoints
{
    public static RouteGroupBuilder MapKnowledgeDashboardEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/dashboard/knowledge");

        group.MapGet("/", GetRecentItems);
        group.MapGet("/search", SearchItems);
        group.MapGet("/tags", GetTags);

        return group;
    }

    internal static async Task<IResult> GetRecentItems(
        KnowledgeReadDbContext db,
        string? type,
        int? limit,
        CancellationToken cancellationToken)
    {
        int take = limit ?? 20;

        IQueryable<KnowledgeItem> query = db.KnowledgeItems
            .Include(i => i.Tags)
            .AsNoTracking();

        if (type is not null && Enum.TryParse<KnowledgeItemType>(type, ignoreCase: true, out KnowledgeItemType itemType))
        {
            query = query.Where(i => i.Type == itemType);
        }

        List<KnowledgeItem> items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = items.Select(ToSummary).ToList();

        return Results.Ok(summaries);
    }

    internal static async Task<IResult> SearchItems(
        KnowledgeReadDbContext db,
        string q,
        string? type,
        string? tag,
        int? limit,
        CancellationToken cancellationToken)
    {
        int take = limit ?? 20;
        string searchTerm = $"%{q}%";

        IQueryable<KnowledgeItem> query = db.KnowledgeItems
            .Include(i => i.Tags)
            .AsNoTracking();

        if (type is not null && Enum.TryParse<KnowledgeItemType>(type, ignoreCase: true, out KnowledgeItemType itemType))
        {
            query = query.Where(i => i.Type == itemType);
        }

        if (tag is not null)
        {
            query = query.Where(i => i.Tags.Any(t => t.Name == tag));
        }

        query = query.Where(i =>
            EF.Functions.Like(i.Title, searchTerm) ||
            (i.Description != null && EF.Functions.Like(i.Description, searchTerm)) ||
            (i.Content != null && EF.Functions.Like(i.Content, searchTerm)) ||
            (i.Url != null && EF.Functions.Like(i.Url, searchTerm)));

        List<KnowledgeItem> items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = items.Select(ToSummary).ToList();

        return Results.Ok(summaries);
    }

    internal static async Task<IResult> GetTags(
        KnowledgeReadDbContext db,
        CancellationToken cancellationToken)
    {
        List<KnowledgeTag> tags = await db.KnowledgeTags
            .Include(t => t.Items)
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = tags
            .Select(t => new KnowledgeTagSummary(t.Id, t.Name, t.Items.Count))
            .ToList();

        return Results.Ok(summaries);
    }

    private static KnowledgeItemSummary ToSummary(KnowledgeItem item) =>
        new(
            item.Id,
            item.Type.ToString(),
            item.Title,
            item.Description,
            item.Url,
            item.Content != null && item.Content.Length > 200
                ? item.Content[..200] + "..."
                : item.Content,
            item.Language,
            item.Category,
            item.Tags.Select(t => t.Name).ToArray(),
            item.CreatedAt);
}
