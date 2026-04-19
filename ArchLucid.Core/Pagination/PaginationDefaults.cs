namespace ArchLucid.Core.Pagination;

/// <summary>
/// Shared constants and helpers for pagination query parameters across all list endpoints.
/// </summary>
public static class PaginationDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    /// <summary>
    /// Maximum <c>take</c> / <c>limit</c> for large unpaged list reads (audit, alerts, conversation messages,
    /// admin diagnostics SQL previews, comparison history search limit when non-zero).
    /// </summary>
    public const int MaxListingTake = 500;

    /// <summary>Clamps page and pageSize to safe bounds.</summary>
    public static (int page, int pageSize) Normalize(int page, int pageSize)
    {
        int safePage = Math.Max(page, 1);
        int safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        return (safePage, safePageSize);
    }

    /// <summary>Calculates the skip count from page and pageSize (zero-based offset).</summary>
    public static int ToSkip(int page, int pageSize)
    {
        (int safePage, int safePageSize) = Normalize(page, pageSize);
        return (safePage - 1) * safePageSize;
    }
}
