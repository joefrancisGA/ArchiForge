namespace ArchLucid.Persistence.Coordination.Backfill;

/// <summary>
///     Read-only assessment: can the database move from fallback-permitted mode to
///     relational-only reads (no legacy JSON slice hydration)?
/// </summary>
public interface ICutoverReadinessService
{
    Task<CutoverReadinessReport> AssessAsync(CancellationToken ct);
}
