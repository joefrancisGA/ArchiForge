using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Queries;

/// <summary>
/// Read-only façade over authority stores (runs, linked snapshots, golden manifests) for a <see cref="ScopeContext"/>.
/// </summary>
/// <remarks>
/// SQL-backed: <see cref="DapperAuthorityQueryService"/>; in-memory/tests: <see cref="InMemoryAuthorityQueryService"/>.
/// Primary callers: <c>ArchLucid.Api.Controllers.AuthorityQueryController</c>, <c>ArchLucid.Persistence.Advisory.AdvisoryScanRunner</c>,
/// comparison/replay/export/ask controllers and services that need run + manifest data without duplicating repository orchestration.
/// </remarks>
public interface IAuthorityQueryService
{
    /// <summary>
    /// Lists recent runs for an authority <paramref name="projectId"/> slug (e.g. <c>default</c>), newest first, capped by <paramref name="take"/>.
    /// </summary>
    /// <param name="scope">Tenant/workspace/project scope (must match stored run rows).</param>
    /// <param name="projectId">Authority project slug, not the scope GUID.</param>
    /// <param name="take">Maximum runs to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Summaries with snapshot and manifest ids; may be empty.</returns>
    Task<IReadOnlyList<RunSummaryDto>> ListRunsByProjectAsync(
        ScopeContext scope,
        string projectId,
        int take,
        CancellationToken ct);

    /// <summary>
    /// Lists a page of runs for <paramref name="projectId"/> (newest first) with total count for pagination.
    /// </summary>
    Task<(IReadOnlyList<RunSummaryDto> Items, int TotalCount)> ListRunsByProjectPagedAsync(
        ScopeContext scope,
        string projectId,
        int skip,
        int take,
        CancellationToken ct);

    /// <summary>Loads a single run’s summary by id within <paramref name="scope"/>.</summary>
    /// <returns>The summary, or <see langword="null"/> when the run is missing or out of scope.</returns>
    Task<RunSummaryDto?> GetRunSummaryAsync(
        ScopeContext scope,
        Guid runId,
        CancellationToken ct);

    /// <summary>
    /// Loads the <see cref="RunRecord"/> and, when ids are present, hydrates context/graph/findings/decision trace, golden manifest, and artifact bundle.
    /// </summary>
    /// <returns>Aggregated detail, or <see langword="null"/> when the run is missing or out of scope.</returns>
    /// <remarks>
    /// Missing child rows (e.g. deleted snapshot) surface as <see langword="null"/> properties on <see cref="RunDetailDto"/> rather than failing the whole call.
    /// The artifact bundle is resolved with <c>GetByManifestIdAsync</c> whenever <see cref="RunRecord.GoldenManifestId"/> is set;
    /// <see cref="RunRecord.ArtifactBundleId"/> is optional denormalization and is not required to load the bundle for replay/export/detail.
    /// </remarks>
    Task<RunDetailDto?> GetRunDetailAsync(
        ScopeContext scope,
        Guid runId,
        CancellationToken ct);

    /// <summary>
    /// Projects a golden manifest into a compact summary (counts and metadata) without returning the full document.
    /// </summary>
    /// <returns>Summary DTO, or <see langword="null"/> when the manifest id is unknown in <paramref name="scope"/>.</returns>
    Task<ManifestSummaryDto?> GetManifestSummaryAsync(
        ScopeContext scope,
        Guid manifestId,
        CancellationToken ct);
}
