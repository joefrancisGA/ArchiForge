namespace ArchiForge.Persistence.Backfill;

/// <summary>
/// Readiness assessment for a single relational child-table slice (e.g. "ContextSnapshot.CanonicalObjects").
/// </summary>
public sealed class CutoverSliceReadiness
{
    /// <summary>
    /// Logical name matching the slice labels used by <see cref="ArchiForge.Persistence.RelationalRead.JsonFallbackPolicy"/>
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
    /// meaning this slice is safe for <see cref="ArchiForge.Persistence.RelationalRead.PersistenceReadMode.RequireRelational"/>.
    /// </summary>
    public bool IsReady => HeadersMissingRelationalRows == 0;
}
