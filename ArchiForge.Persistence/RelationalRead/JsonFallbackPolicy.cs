using ArchiForge.Core.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.Persistence.RelationalRead;

/// <summary>
/// Centralizes the decision to allow, warn on, or reject JSON-column fallback reads
/// when relational child tables are empty.
/// </summary>
/// <remarks>
/// <para><b>Default:</b> <see cref="PersistenceReadMode.AllowJsonFallback"/> — preserves legacy
/// behavior. All persistence code that branches on "relational rows vs JSON column" must route
/// through <see cref="EvaluateFallback"/> so the mode decision is in one place.</para>
/// <para>Diagnostics: every fallback event increments
/// <see cref="ArchiForgeInstrumentation.JsonFallbackUsed"/> (OTel counter) and emits a
/// structured log at <c>Debug</c> (allow) or <c>Warning</c> (warn) level.</para>
/// <para>Wire as singleton in DI; the backfill tooling and repositories share one instance.</para>
/// </remarks>
public sealed class JsonFallbackPolicy(PersistenceReadMode mode, ILogger logger)
{
    public JsonFallbackPolicy()
        : this(PersistenceReadMode.AllowJsonFallback, NullLogger.Instance)
    {
    }

    public PersistenceReadMode Mode { get; } = mode;

    /// <summary>
    /// Backward-compatible property; <c>true</c> when mode is not <see cref="PersistenceReadMode.RequireRelational"/>.
    /// </summary>
    public bool AllowFallback => Mode != PersistenceReadMode.RequireRelational;

    /// <summary>
    /// Evaluates whether the caller should fall back to the JSON column.
    /// Emits structured diagnostics (log + OTel counter) on every fallback event.
    /// </summary>
    /// <param name="relationalRowCount">Rows found in the relational child table.</param>
    /// <param name="sliceName">Diagnostic label (e.g. "ContextSnapshot.CanonicalObjects").</param>
    /// <param name="entityType">Entity type for diagnostics (e.g. "ContextSnapshot").</param>
    /// <param name="entityId">Entity identifier for diagnostics.</param>
    /// <returns><c>true</c> when the caller should load from JSON.</returns>
    /// <exception cref="RelationalDataMissingException">
    /// Thrown when <see cref="Mode"/> is <see cref="PersistenceReadMode.RequireRelational"/>
    /// and <paramref name="relationalRowCount"/> is zero.
    /// </exception>
    public bool EvaluateFallback(int relationalRowCount, string sliceName, string entityType = "", string entityId = "")
    {
        if (relationalRowCount > 0)
            return false;

        switch (Mode)
        {
            case PersistenceReadMode.AllowJsonFallback:
                RecordFallbackEvent(LogLevel.Debug, sliceName, entityType, entityId);
                return true;

            case PersistenceReadMode.WarnOnJsonFallback:
                RecordFallbackEvent(LogLevel.Warning, sliceName, entityType, entityId);
                return true;

            case PersistenceReadMode.RequireRelational:
                throw new RelationalDataMissingException(entityType, entityId, sliceName);

            default:
                RecordFallbackEvent(LogLevel.Debug, sliceName, entityType, entityId);
                return true;
        }
    }

    /// <summary>
    /// Simplified overload without entity context; kept for callers that only have a slice name.
    /// </summary>
    public bool ShouldFallbackToJson(int relationalRowCount, string sliceName)
    {
        return EvaluateFallback(relationalRowCount, sliceName);
    }

    private void RecordFallbackEvent(LogLevel level, string sliceName, string entityType, string entityId)
    {
        string modeLabel = Mode.ToString();

        ArchiForgeInstrumentation.JsonFallbackUsed.Add(
            1,
            new KeyValuePair<string, object?>("entity_type", entityType),
            new KeyValuePair<string, object?>("slice", sliceName),
            new KeyValuePair<string, object?>("read_mode", modeLabel));

        logger.Log(
            level,
            "JSON fallback used — slice={SliceName}, entityType={EntityType}, entityId={EntityId}, " +
            "readMode={ReadMode}. Run SqlRelationalBackfillService to eliminate fallback reads.",
            sliceName, entityType, entityId, modeLabel);
    }
}
