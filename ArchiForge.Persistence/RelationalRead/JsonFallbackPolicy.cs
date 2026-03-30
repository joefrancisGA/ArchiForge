namespace ArchiForge.Persistence.RelationalRead;

/// <summary>
/// Centralizes the decision to allow or deny JSON-column fallback reads when relational
/// child tables are empty.
/// </summary>
/// <remarks>
/// <para><b>Default:</b> <see cref="AllowFallback"/> = <c>true</c> — preserves legacy behavior
/// (relational-first, JSON if no relational rows). Set to <c>false</c> to force relational-only
/// reads and surface missing migrations as empty collections instead of silently loading JSON.</para>
/// <para>All persistence code that branches on "relational rows vs JSON column" must route
/// through <see cref="ShouldFallbackToJson"/> so the cutover decision is in one place.</para>
/// <para>Wire as singleton in DI; the backfill tooling and repositories share one instance.</para>
/// </remarks>
public sealed class JsonFallbackPolicy
{
    /// <summary>
    /// When <c>true</c> (default), empty relational slices fall back to JSON columns.
    /// When <c>false</c>, empty relational slices return empty/default collections.
    /// </summary>
    public bool AllowFallback { get; init; } = true;

    /// <summary>
    /// Returns <c>true</c> when the caller should load from the JSON column.
    /// </summary>
    /// <param name="relationalRowCount">Number of rows found in the relational child table.</param>
    /// <param name="sliceName">Diagnostic label for logging/telemetry (e.g. "ContextSnapshot.CanonicalObjects").</param>
    public bool ShouldFallbackToJson(int relationalRowCount, string sliceName)
    {
        _ = sliceName;

        if (relationalRowCount > 0)
            return false;

        return AllowFallback;
    }
}
