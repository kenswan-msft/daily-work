using DailyWork.Web.Models;

namespace DailyWork.Web.Services;

public class DashboardApiClient(HttpClient httpClient)
{
    public virtual async Task<DashboardOverview?> GetOverviewAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<DashboardOverview>(
            "/api/dashboard/overview", cancellationToken).ConfigureAwait(false);

    public virtual async Task<List<GoalSummary>> GetGoalsAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        string url = "/api/dashboard/goals";
        if (status is not null)
        {
            url += $"?status={status}";
        }

        return await httpClient.GetFromJsonAsync<List<GoalSummary>>(url, cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    public virtual async Task<GoalProgressSummary?> GetGoalProgressAsync(
        Guid goalId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<GoalProgressSummary>(
            $"/api/dashboard/goals/{goalId}/progress", cancellationToken).ConfigureAwait(false);

    public virtual async Task<List<TodoSummary>> GetTodosAsync(
        string? status = null,
        string? priority = null,
        bool? overdue = null,
        bool? dueToday = null,
        CancellationToken cancellationToken = default)
    {
        List<string> queryParams = [];
        if (status is not null)
        {
            queryParams.Add($"status={status}");
        }

        if (priority is not null)
        {
            queryParams.Add($"priority={priority}");
        }

        if (overdue == true)
        {
            queryParams.Add("overdue=true");
        }

        if (dueToday == true)
        {
            queryParams.Add("dueToday=true");
        }

        string url = "/api/dashboard/todos";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await httpClient.GetFromJsonAsync<List<TodoSummary>>(url, cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    public virtual async Task<List<DailyFocusItem>> GetFocusItemsAsync(
        int? maxItems = null,
        CancellationToken cancellationToken = default)
    {
        string url = "/api/dashboard/focus";
        if (maxItems.HasValue)
        {
            url += $"?maxItems={maxItems.Value}";
        }

        return await httpClient.GetFromJsonAsync<List<DailyFocusItem>>(url, cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    public virtual async Task<List<KnowledgeItemSummary>> GetKnowledgeItemsAsync(
        string? type = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        List<string> queryParams = [];
        if (type is not null)
        {
            queryParams.Add($"type={type}");
        }

        if (limit.HasValue)
        {
            queryParams.Add($"limit={limit.Value}");
        }

        string url = "/api/dashboard/knowledge";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await httpClient.GetFromJsonAsync<List<KnowledgeItemSummary>>(url, cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    public virtual async Task<List<KnowledgeItemSummary>> SearchKnowledgeAsync(
        string query,
        string? type = null,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        List<string> queryParams = [$"q={Uri.EscapeDataString(query)}"];
        if (type is not null)
        {
            queryParams.Add($"type={type}");
        }

        if (tag is not null)
        {
            queryParams.Add($"tag={Uri.EscapeDataString(tag)}");
        }

        string url = "/api/dashboard/knowledge/search?" + string.Join("&", queryParams);

        return await httpClient.GetFromJsonAsync<List<KnowledgeItemSummary>>(url, cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    public virtual async Task<List<KnowledgeTagSummary>> GetKnowledgeTagsAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<KnowledgeTagSummary>>(
            "/api/dashboard/knowledge/tags", cancellationToken).ConfigureAwait(false) ?? [];
}
