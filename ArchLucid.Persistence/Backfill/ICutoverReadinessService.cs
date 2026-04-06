namespace ArchiForge.Persistence.Backfill;

/// <summary>
/// Read-only assessment: can the database move from fallback-permitted mode to
/// <see cref="ArchiForge.Persistence.RelationalRead.PersistenceReadMode.RequireRelational"/>?
/// </summary>
public interface ICutoverReadinessService
{
    Task<CutoverReadinessReport> AssessAsync(CancellationToken ct);
}
