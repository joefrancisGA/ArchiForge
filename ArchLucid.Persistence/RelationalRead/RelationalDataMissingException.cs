namespace ArchiForge.Persistence.RelationalRead;

/// <summary>
/// Thrown when <see cref="PersistenceReadMode.RequireRelational"/> is active and a
/// relational child table has no rows for a slice that should have been backfilled.
/// </summary>
public sealed class RelationalDataMissingException(
    string entityType,
    string entityId,
    string sliceName)
    : InvalidOperationException(BuildMessage(entityType, entityId, sliceName))
{
    public string EntityType { get; } = entityType;
    public string EntityId { get; } = entityId;
    public string SliceName { get; } = sliceName;

    private static string BuildMessage(string entityType, string entityId, string sliceName) =>
        $"Relational data missing for {entityType} '{entityId}', slice '{sliceName}'. " +
        "PersistenceReadMode is RequireRelational. " +
        "Run SqlRelationalBackfillService or re-save the entity to populate relational child tables.";
}
