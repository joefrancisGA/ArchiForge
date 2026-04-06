namespace ArchiForge.Core.Pagination;

/// <summary>
/// Standard pagination envelope for list endpoints.
/// Controllers that adopt pagination return this instead of a bare <c>IReadOnlyList&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class PagedResponse<T>
{
    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Total item count across all pages (when the repository supports counting).</summary>
    public int TotalCount { get; init; }

    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Maximum items per page.</summary>
    public int PageSize { get; init; } = 50;

    /// <summary>Whether additional pages exist beyond the current page.</summary>
    public bool HasMore => Page * PageSize < TotalCount;
}
