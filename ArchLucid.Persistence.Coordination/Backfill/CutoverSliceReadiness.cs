namespace ArchLucid.Persistence.Coordination.Backfill;

/// <summary>
/// Readiness assessment for a single relational child-table slice (e.g. "ContextSnapshot.CanonicalObjects").
/// </summary>
public sealed class CutoverSliceReadiness
{
    /// <summary>
    /// Logical name for cutover reporting (matches historical slice labels, e.g. <c>ContextSnapshot.CanonicalObjects</c>).
    /// (e.g. "ContextSnapshot.CanonicalObjects").
    /// </summary>
    public required string SliceName { get; init; }

    /// <summary>Total header rows in the parent table for this entity type.</summary>
    public int TotalHeaderRows { get; init; }

    /// <summary>Header rows that have at least one relational child row in this slice.</summary>
    public int HeadersWithRelationalRows { get; init; }

    /// <summary>Header rows with no relational child rows — these still depend on JSON fallback.</summary>
    public int HeadersMissingRelationalRows => TotalHeaderRows - HeadersWithRelationalRows;

    /// <summary>
    /// <c>true</c> when every header row has at least one relational child row,
    /// meaning every header row has relational child data for this slice (safe for relational-only reads).
    /// </summary>
    public bool IsReady => HeadersMissingRelationalRows == 0;
}
