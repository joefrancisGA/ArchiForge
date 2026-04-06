namespace ArchiForge.Persistence.RelationalRead;

/// <summary>
/// Controls how the persistence layer behaves when relational child tables are empty
/// and a JSON-column fallback exists.
/// </summary>
public enum PersistenceReadMode
{
    /// <summary>
    /// Default. Fall back to JSON columns silently when relational child rows are absent.
    /// This is the legacy behavior preserved for backward compatibility.
    /// </summary>
    AllowJsonFallback = 0,

    /// <summary>
    /// Fall back to JSON columns, but emit a structured warning each time fallback is used.
    /// Useful during migration roll-out to monitor how much JSON is still being read.
    /// </summary>
    WarnOnJsonFallback = 1,

    /// <summary>
    /// Require relational data. If relational child rows are absent for a slice that is
    /// expected to be relationalized, throw <see cref="RelationalDataMissingException"/>.
    /// Use after confirming all environments are fully backfilled.
    /// </summary>
    RequireRelational = 2,
}
