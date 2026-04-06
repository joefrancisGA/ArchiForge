namespace ArchiForge.Persistence.RelationalRead;

/// <summary>
/// Centralizes the relational-first / JSON fallback branch for a single slice.
/// The fallback decision is governed by <see cref="JsonFallbackPolicy"/>.
/// </summary>
internal static class RelationalFirstRead
{
    /// <summary>
    /// Loads from relational tables when rows exist; otherwise consults
    /// <paramref name="policy"/> to decide whether to fall back, warn, or throw.
    /// </summary>
    /// <param name="relationalRowCount">COUNT(1) from the relational child table.</param>
    /// <param name="sliceName">Diagnostic label passed to <see cref="JsonFallbackPolicy.EvaluateFallback"/>.</param>
    /// <param name="loadRelational">Async loader for relational data.</param>
    /// <param name="loadJsonFallback">Sync loader for JSON column data (legacy).</param>
    /// <param name="emptyDefault">Value to return when fallback is denied and relational data is empty.</param>
    /// <param name="policy">Fallback policy; when <c>null</c>, fallback is always allowed (backward compat).</param>
    /// <param name="entityType">Entity type for diagnostics (e.g. "ContextSnapshot").</param>
    /// <param name="entityId">Entity identifier for diagnostics.</param>
    internal static async Task<T> ReadSliceAsync<T>(
        int relationalRowCount,
        string sliceName,
        Func<Task<T>> loadRelational,
        Func<T> loadJsonFallback,
        Func<T> emptyDefault,
        JsonFallbackPolicy? policy,
        string entityType = "",
        string entityId = "")
    {
        if (relationalRowCount > 0)
            return await loadRelational();

        if (policy is null)
            return loadJsonFallback();

        return policy.EvaluateFallback(relationalRowCount, sliceName, entityType, entityId) ? loadJsonFallback() : emptyDefault();
    }

    /// <summary>
    /// Backward-compatible overload: always allows fallback (no policy).
    /// </summary>
    internal static Task<T> ReadSliceAsync<T>(
        int relationalRowCount,
        Func<Task<T>> loadRelational,
        Func<T> loadJsonFallback)
    {
        return ReadSliceAsync(
            relationalRowCount,
            sliceName: "unknown",
            loadRelational,
            loadJsonFallback,
            emptyDefault: loadJsonFallback,
            policy: null);
    }
}
