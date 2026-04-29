namespace ArchLucid.Core.Pagination;

/// <summary>
///     Stable keyset pagination (cursor + take) — no OFFSET and no total count query.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class CursorPagedResponse<T>
{
    /// <summary>Items returned (at most <see cref="RequestedTake" />).</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    ///     Opaque encoded cursor (<see cref="RunCursorCodec" />) for the client to pass back for the next page;
    ///     <see langword="null" /> when no further pages (<see cref="HasMore" /> false).
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>Whether more rows may exist beyond this page (<c>Items.Count == Take</c>).</summary>
    public bool HasMore { get; init; }

    /// <summary>The effective take/maximum row count requested for this call.</summary>
    public int RequestedTake { get; init; }
}
