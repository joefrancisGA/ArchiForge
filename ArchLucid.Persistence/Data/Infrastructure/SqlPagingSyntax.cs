namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
///     SQL Server paging fragment appended after <c>ORDER BY</c>.
/// </summary>
public static class SqlPagingSyntax
{
    /// <summary>
    ///     Fragment to append after <c>ORDER BY</c>. <paramref name="rowCount" /> must be a trusted constant from code.
    /// </summary>
    public static string FirstRowsOnly(int rowCount)
    {
        return rowCount <= 0
            ? throw new ArgumentOutOfRangeException(nameof(rowCount))
            : $"OFFSET 0 ROWS FETCH NEXT {rowCount} ROWS ONLY";
    }
}
