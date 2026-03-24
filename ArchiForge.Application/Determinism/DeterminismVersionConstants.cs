namespace ArchiForge.Application.Determinism;

/// <summary>
/// Manifest version string patterns used by <see cref="DeterminismCheckService"/> when
/// <c>commitReplays</c> is enabled. Centralised here to avoid typos and to make
/// stored version strings easy to trace back to the determinism pipeline.
/// </summary>
public static class DeterminismVersionConstants
{
    /// <summary>
    /// Manifest version assigned to the first (baseline) replay within a determinism check.
    /// </summary>
    public const string BaselineVersion = "determinism-baseline";

    /// <summary>
    /// Format string for iteration replay manifests. <c>{0}</c> is the 1-based iteration number.
    /// </summary>
    public const string IterationVersionFormat = "determinism-{0}";

    /// <summary>
    /// Returns the manifest version string for the given 1-based <paramref name="iteration"/> number.
    /// </summary>
    public static string IterationVersion(int iteration) =>
        string.Format(IterationVersionFormat, iteration);
}
