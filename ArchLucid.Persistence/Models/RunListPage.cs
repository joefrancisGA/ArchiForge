namespace ArchLucid.Persistence.Models;

/// <summary>
///     Results of a keyset-paged run query: items and whether more rows exist (<see cref="HasMore" />).
/// </summary>
public sealed record RunListPage(IReadOnlyList<RunRecord> Items, bool HasMore);
