using ArchLucid.Core.Authorization;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Pagination;
using ArchLucid.Api.Services.Admin;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Models;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.FeatureManagement;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>Operator diagnostics (outbox depth, leader leases, feature flags).</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin")]
public sealed class AdminController(
    IAdminDiagnosticsService diagnostics,
    IFeatureManager featureManager) : ControllerBase
{
    private readonly IAdminDiagnosticsService _diagnostics =
        diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

    private readonly IFeatureManager _featureManager =
        featureManager ?? throw new ArgumentNullException(nameof(featureManager));

    /// <summary>Pending asynchronous authority and retrieval indexing work.</summary>
    [HttpGet("diagnostics/outboxes")]
    [ProducesResponseType(typeof(AdminOutboxSnapshot), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxes(CancellationToken cancellationToken = default)
    {
        AdminOutboxSnapshot snapshot = await _diagnostics.GetOutboxSnapshotAsync(cancellationToken);

        return Ok(snapshot);
    }

    /// <summary>SQL host leader lease holders (empty when InMemory storage or election disabled).</summary>
    [HttpGet("diagnostics/leases")]
    [ProducesResponseType(typeof(IReadOnlyList<HostLeaderLeaseSnapshot>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeases(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HostLeaderLeaseSnapshot> rows =
            await _diagnostics.GetLeasesAsync(cancellationToken);

        return Ok(rows);
    }

    /// <summary>Effective state of the async authority pipeline feature flag.</summary>
    [HttpGet("features/async-authority-pipeline")]
    [ProducesResponseType(typeof(AsyncAuthorityPipelineFeatureState), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsyncAuthorityPipelineFeature(CancellationToken cancellationToken = default)
    {
        bool enabled =
            await _featureManager.IsEnabledAsync(AuthorityPipelineFeatureFlags.AsyncAuthorityPipeline, cancellationToken);

        return Ok(new AsyncAuthorityPipelineFeatureState(enabled));
    }

    /// <summary>Integration event outbox rows that exceeded publish retries (inspect before manual retry).</summary>
    [HttpGet("integration-outbox/dead-letters")]
    [ProducesResponseType(typeof(IReadOnlyList<IntegrationEventOutboxDeadLetterRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListIntegrationOutboxDeadLetters(
        [FromQuery] int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IntegrationEventOutboxDeadLetterRow> rows =
            await _diagnostics.ListIntegrationOutboxDeadLettersAsync(maxRows, cancellationToken);

        return Ok(rows);
    }

    /// <summary>Detection-only orphan counts (same SQL as the background data-consistency probe).</summary>
    [HttpGet("diagnostics/data-consistency/orphans")]
    [ProducesResponseType(typeof(DataConsistencyOrphanCounts), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDataConsistencyOrphans(CancellationToken cancellationToken = default)
    {
        DataConsistencyOrphanCounts counts =
            await _diagnostics.GetDataConsistencyOrphanCountsAsync(cancellationToken);

        return Ok(counts);
    }

    /// <summary>
    /// Lists or deletes orphan <c>ComparisonRecords</c> whose run ids are missing from <c>dbo.Runs</c>.
    /// Use <c>dryRun=true</c> first. Capped at <see cref="PaginationDefaults.MaxListingTake"/> rows per call.
    /// </summary>
    [HttpPost("diagnostics/data-consistency/orphan-comparison-records")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(OrphanComparisonRemediationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemediateOrphanComparisonRecords(
        [FromQuery] bool dryRun = true,
        [FromQuery] int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        OrphanComparisonRemediationResult result =
            await _diagnostics.RemediateOrphanComparisonRecordsAsync(dryRun, maxRows, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Lists or deletes orphan <c>dbo.GoldenManifests</c> (missing <c>dbo.Runs</c>), removing <c>dbo.ArtifactBundles</c> first.
    /// Use <c>dryRun=true</c> first. Capped at <see cref="PaginationDefaults.MaxListingTake"/> rows per call.
    /// </summary>
    [HttpPost("diagnostics/data-consistency/orphan-golden-manifests")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(OrphanGoldenManifestRemediationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemediateOrphanGoldenManifests(
        [FromQuery] bool dryRun = true,
        [FromQuery] int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        OrphanGoldenManifestRemediationResult result =
            await _diagnostics.RemediateOrphanGoldenManifestsAsync(dryRun, maxRows, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Lists or deletes orphan <c>dbo.FindingsSnapshots</c> (missing run, not referenced by a golden manifest).
    /// Use <c>dryRun=true</c> first. Capped at <see cref="PaginationDefaults.MaxListingTake"/> rows per call.
    /// </summary>
    [HttpPost("diagnostics/data-consistency/orphan-findings-snapshots")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(OrphanFindingsSnapshotRemediationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemediateOrphanFindingsSnapshots(
        [FromQuery] bool dryRun = true,
        [FromQuery] int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        OrphanFindingsSnapshotRemediationResult result =
            await _diagnostics.RemediateOrphanFindingsSnapshotsAsync(dryRun, maxRows, cancellationToken);

        return Ok(result);
    }

    /// <summary>Soft-archives authority runs created strictly before the cutoff (operator-initiated bulk archival).</summary>
    [HttpPost("runs/archive-batch")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(RunArchiveBatchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ArchiveRunsBatch(
        [FromBody] AdminArchiveRunsBatchRequest? body,
        CancellationToken cancellationToken = default)
    {
        if (body is null)
        {
            return this.BadRequestProblem("Request body is required.", ProblemTypes.ValidationFailed);
        }

        if (body.CreatedBeforeUtc == default)
        {
            return this.BadRequestProblem("CreatedBeforeUtc must be set.", ProblemTypes.ValidationFailed);
        }

        RunArchiveBatchResult result =
            await _diagnostics.ArchiveRunsCreatedBeforeAsync(body.CreatedBeforeUtc, cancellationToken);

        return Ok(result);
    }

    /// <summary>Soft-archives specific runs by id (partial success: per-id failures returned in the body).</summary>
    [HttpPost("runs/archive-by-ids")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(RunArchiveByIdsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ArchiveRunsByIds(
        [FromBody] AdminArchiveRunsByIdsRequest? body,
        CancellationToken cancellationToken = default)
    {
        if (body is null)
        {
            return this.BadRequestProblem("Request body is required.", ProblemTypes.ValidationFailed);
        }

        if (body.RunIds is null || body.RunIds.Count == 0)
        {
            return this.BadRequestProblem("RunIds must contain at least one id.", ProblemTypes.ValidationFailed);
        }

        if (body.RunIds.Count > 100)
        {
            return this.BadRequestProblem("At most 100 run ids are allowed per request.", ProblemTypes.ValidationFailed);
        }

        RunArchiveByIdsResult result =
            await _diagnostics.ArchiveRunsByIdsAsync(body.RunIds, cancellationToken);

        return Ok(result);
    }

    /// <summary>Clears dead-letter state for one outbox row so the worker will publish again.</summary>
    [HttpPost("integration-outbox/dead-letters/{outboxId:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryIntegrationOutboxDeadLetter(
        Guid outboxId,
        CancellationToken cancellationToken = default)
    {
        bool ok = await _diagnostics.RetryIntegrationOutboxDeadLetterAsync(outboxId, cancellationToken);

        if (!ok)
        {
            return this.NotFoundProblem(
                $"Integration outbox dead-letter row '{outboxId:D}' was not found.",
                ProblemTypes.ResourceNotFound);
        }

        return NoContent();
    }
}

/// <summary>JSON body for <c>GET .../features/async-authority-pipeline</c>.</summary>
public sealed record AsyncAuthorityPipelineFeatureState(bool Enabled);
