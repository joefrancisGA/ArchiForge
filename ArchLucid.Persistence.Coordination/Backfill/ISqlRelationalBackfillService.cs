namespace ArchLucid.Persistence.Coordination.Backfill;

/// <summary>
///     One-time SQL Server utility: deserialize legacy JSON columns and insert missing relational rows (dual-write
///     alignment).
/// </summary>
public interface ISqlRelationalBackfillService
{
    Task<SqlRelationalBackfillReport> RunAsync(SqlRelationalBackfillOptions options, CancellationToken ct);
}
