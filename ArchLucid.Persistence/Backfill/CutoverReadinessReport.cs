namespace ArchiForge.Persistence.Backfill;

/// <summary>
/// Aggregate readiness report across all authority types and slices.
/// Answers: "Is the database ready to switch to <see cref="ArchiForge.Persistence.RelationalRead.PersistenceReadMode.RequireRelational"/>?"
/// </summary>
public sealed class CutoverReadinessReport
{
    /// <summary>Per-slice readiness assessments, one entry per assessed slice.</summary>
    public IReadOnlyList<CutoverSliceReadiness> Slices { get; init; } = [];

    /// <summary>
    /// <c>true</c> only when every slice reports <see cref="CutoverSliceReadiness.IsReady"/>.
    /// </summary>
    public bool IsFullyReady => Slices.All(static s => s.IsReady);

    /// <summary>Total header rows across all entity types (sum of per-slice totals, deduplicated by entity type).</summary>
    public int TotalHeaderRows => Slices
        .GroupBy(static s => EntityTypeFromSlice(s.SliceName))
        .Sum(static g => g.First().TotalHeaderRows);

    /// <summary>Slices that still have at least one header row missing relational data.</summary>
    public IReadOnlyList<CutoverSliceReadiness> SlicesNotReady => Slices.Where(static s => !s.IsReady).ToList();

    /// <summary>Extracts "ContextSnapshot" from "ContextSnapshot.CanonicalObjects".</summary>
    private static string EntityTypeFromSlice(string sliceName)
    {
        int dot = sliceName.IndexOf('.');

        return dot > 0 ? sliceName[..dot] : sliceName;
    }
}
