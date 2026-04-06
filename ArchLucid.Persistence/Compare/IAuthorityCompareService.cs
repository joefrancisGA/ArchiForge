using ArchiForge.Core.Scoping;

using JetBrains.Annotations;

namespace ArchiForge.Persistence.Compare;

/// <summary>
/// Scope-safe structural comparison of golden manifests and authority runs (diff lists for UI and advisory).
/// </summary>
/// <remarks>
/// Implementation: <see cref="AuthorityCompareService"/>. HTTP: <c>ArchiForge.Api.Controllers.AuthorityCompareController</c>.
/// </remarks>
public interface IAuthorityCompareService
{
    /// <summary>
    /// Appends a single <see cref="DiffKind.Changed"/> <see cref="DiffItem"/> when <paramref name="beforeValue"/> and <paramref name="afterValue"/> differ (ordinal).
    /// </summary>
    /// <remarks>No-op when values are equal; used for run-level fields and available for custom comparers.</remarks>
    /// <param name="diffs">Mutable list to append to.</param>
    /// <param name="section">Logical grouping label (e.g. <c>Run</c>).</param>
    /// <param name="key">Field name within the section.</param>
    /// <param name="beforeValue">Left-hand value; may be <see langword="null"/>.</param>
    /// <param name="afterValue">Right-hand value; may be <see langword="null"/>.</param>
    [UsedImplicitly]
    void AddRunDiff(IList<DiffItem> diffs, string section, string key, string? beforeValue, string? afterValue);

    /// <summary>
    /// Loads both manifests in <paramref name="scope"/> and produces a flat list of added/removed/changed facets.
    /// </summary>
    /// <returns>Comparison result, or <see langword="null"/> if either manifest is missing or tenant/workspace/project do not match across manifests.</returns>
    /// <param name="scope">Caller scope for tenant/workspace/project isolation.</param>
    /// <param name="leftManifestId">First manifest id (baseline).</param>
    /// <param name="rightManifestId">Second manifest id (target).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ManifestComparisonResult?> CompareManifestsAsync(
        ScopeContext scope,
        Guid leftManifestId,
        Guid rightManifestId,
        CancellationToken ct);

    /// <summary>
    /// Compares two runs’ summaries and, when both reference golden manifests, nests <see cref="CompareManifestsAsync"/>.
    /// </summary>
    /// <returns>Result with run-level diffs and optional manifest comparison, or <see langword="null"/> if either run summary is missing.</returns>
    /// <param name="scope">Caller scope for tenant/workspace/project isolation.</param>
    /// <param name="leftRunId">Baseline run id.</param>
    /// <param name="rightRunId">Target run id.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<RunComparisonResult?> CompareRunsAsync(
        ScopeContext scope,
        Guid leftRunId,
        Guid rightRunId,
        CancellationToken ct);
}
