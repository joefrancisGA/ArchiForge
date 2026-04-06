namespace ArchiForge.Core.Pagination;

/// <summary>
/// Builds <see cref="PagedResponse{T}"/> from an in-memory collection or from database page + total count.
/// </summary>
public static class PagedResponseBuilder
{
    /// <summary>
    /// Applies in-memory skip/take over <paramref name="allItems"/> and wraps the result in a <see cref="PagedResponse{T}"/>.
    /// </summary>
    public static PagedResponse<T> Build<T>(IReadOnlyList<T> allItems, int page, int pageSize)
    {
        (int safePage, int safePageSize) = PaginationDefaults.Normalize(page, pageSize);
        int skip = PaginationDefaults.ToSkip(safePage, safePageSize);

        IReadOnlyList<T> items = allItems
            .Skip(skip)
            .Take(safePageSize)
            .ToList();

        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = allItems.Count,
            Page = safePage,
            PageSize = safePageSize
        };
    }

    /// <summary>
    /// Wraps a database page (items already limited by OFFSET/FETCH or equivalent) with total count from the server.
    /// </summary>
    public static PagedResponse<T> FromDatabasePage<T>(
        IReadOnlyList<T> pageItems,
        int totalCount,
        int page,
        int pageSize)
    {
        (int safePage, int safePageSize) = PaginationDefaults.Normalize(page, pageSize);

        return new PagedResponse<T>
        {
            Items = pageItems,
            TotalCount = totalCount,
            Page = safePage,
            PageSize = safePageSize
        };
    }
}
