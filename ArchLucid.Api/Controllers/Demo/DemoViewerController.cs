using ArchLucid.Api.Contracts;
using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Api.Support;
using ArchLucid.Application;
using ArchLucid.Application.Architecture;
using ArchLucid.Application.Bootstrap;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Persistence.Coordination.Compare;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers.Demo;

/// <summary>
///     Read-only anonymous viewer for Contoso demo-seeded data when <c>Demo:AnonymousViewer:Enabled</c> is true.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/demo/viewer")]
[EnableRateLimiting("fixed")]
[AllowAnonymous]
public sealed class DemoViewerController(
    IOptions<DemoOptions> demoOptions,
    IRunDetailQueryService runDetailQueryService,
    IArchitectureRunProvenanceService architectureRunProvenanceService,
    IAuthorityCompareService authorityCompareService,
    IConfiguration configuration) : ControllerBase
{
    private readonly IArchitectureRunProvenanceService _architectureRunProvenanceService =
        architectureRunProvenanceService ?? throw new ArgumentNullException(nameof(architectureRunProvenanceService));

    private readonly IAuthorityCompareService _authorityCompareService =
        authorityCompareService ?? throw new ArgumentNullException(nameof(authorityCompareService));

    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly IOptions<DemoOptions> _demoOptions =
        demoOptions ?? throw new ArgumentNullException(nameof(demoOptions));

    private readonly IRunDetailQueryService _runDetailQueryService =
        runDetailQueryService ?? throw new ArgumentNullException(nameof(runDetailQueryService));

    /// <summary>Lists recent runs in the Contoso demo scope.</summary>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(List<RunListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListRuns(CancellationToken cancellationToken)
    {
        if (!IsViewerAllowed())
            return Unauthorized();

        using IDisposable _ = AmbientScopeContext.Push(DemoScopes.BuildDemoScope());
        IReadOnlyList<RunSummary> summaries = await _runDetailQueryService.ListRunSummariesAsync(cancellationToken);

        List<RunListItemResponse> response = summaries
            .Select(r => new RunListItemResponse
            {
                RunId = r.RunId,
                RequestId = r.RequestId,
                Status = r.Status,
                CreatedUtc = r.CreatedUtc,
                CompletedUtc = r.CompletedUtc,
                CurrentManifestVersion = r.CurrentManifestVersion,
                SystemName = r.SystemName
            })
            .ToList();

        return Ok(response);
    }

    /// <summary>Run aggregate (same shape as <c>GET /v1/architecture/run/{runId}</c>).</summary>
    [HttpGet("runs/{runId}")]
    [ProducesResponseType(typeof(RunDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(string runId, CancellationToken cancellationToken)
    {
        if (!IsViewerAllowed())
            return Unauthorized();

        using IDisposable _ = AmbientScopeContext.Push(DemoScopes.BuildDemoScope());

        ArchitectureRunDetail? detail = await _runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found (or is out of scope).", ProblemTypes.RunNotFound);

        if (!string.IsNullOrWhiteSpace(detail.Run.CurrentManifestVersion) && detail.Manifest is null)
            return this.NotFoundProblem($"Manifest for run '{runId}' was not found.", ProblemTypes.ManifestNotFound);

        RunDetailsResponse response = RunResponseMapper.ToRunDetailsResponse(
            detail.Run,
            detail.Tasks,
            detail.Results,
            detail.Manifest,
            detail.DecisionTraces);

        response.ExecutionFlavorBuyerSummary = RunExecutionFlavorSummary.Build(
            detail.Run,
            _configuration["AgentExecution:Mode"]);

        return Ok(response);
    }

    /// <summary>Provenance graph for one run (same payload as <c>GET /v1/architecture/runs/{runId}/provenance</c>).</summary>
    [HttpGet("runs/{runId}/graph")]
    [ProducesResponseType(typeof(ArchitectureRunProvenanceGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGraph(string runId, CancellationToken cancellationToken)
    {
        if (!IsViewerAllowed())
            return Unauthorized();

        using IDisposable _ = AmbientScopeContext.Push(DemoScopes.BuildDemoScope());

        ArchitectureRunProvenanceGraph? graph =
            await _architectureRunProvenanceService.GetProvenanceAsync(runId, cancellationToken);

        return graph is null
            ? this.NotFoundProblem($"Provenance graph for run '{runId}' was not found.", ProblemTypes.ResourceNotFound)
            : Ok(graph);
    }

    /// <summary>
    ///     Compares two runs; defaults to Contoso baseline vs hardened GUIDs when query params omitted (single-catalog
    ///     seed).
    /// </summary>
    [HttpGet("compare")]
    [ProducesResponseType(typeof(RunComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareRuns(
        [FromQuery] Guid? leftRunId,
        [FromQuery] Guid? rightRunId,
        CancellationToken cancellationToken = default)
    {
        if (!IsViewerAllowed())
            return Unauthorized();

        Guid left = leftRunId ?? ContosoRetailDemoIdentifiers.AuthorityRunBaselineId;
        Guid right = rightRunId ?? ContosoRetailDemoIdentifiers.AuthorityRunHardenedId;

        using IDisposable _ = AmbientScopeContext.Push(DemoScopes.BuildDemoScope());

        RunComparisonResult? result =
            await _authorityCompareService.CompareRunsAsync(
                DemoScopes.BuildDemoScope(),
                left,
                right,
                cancellationToken);

        if (result is null)
            return this.NotFoundProblem(
                $"One or both runs ('{left}', '{right}') were not found in the demo scope.",
                ProblemTypes.RunNotFound);

        return Ok(
            new RunComparisonResponse
            {
                LeftRunId = result.LeftRunId,
                RightRunId = result.RightRunId,
                RunLevelDiffs = result.RunLevelDiffs.Select(MapDiffItem).ToList(),
                ManifestComparison =
                    result.ManifestComparison is null ? null : MapManifestResponse(result.ManifestComparison),
                RunLevelDiffCount = result.RunLevelDiffs.Count,
                HasManifestComparison = result.ManifestComparison is not null
            });
    }

    /// <summary>POST is not supported on the anonymous viewer.</summary>
    /// <remarks>
    ///     <c>[AcceptVerbs("POST")]</c> keeps POST routing; CI treats <c>[HttpPost]</c> as mutating and would require
    ///     audit for this non-mutating 405 handler.
    /// </remarks>
    [AcceptVerbs("POST")]
    [Route("{*catchAll}")]
    [ProducesResponseType(StatusCodes.Status405MethodNotAllowed)]
    public IActionResult PostNotAllowed()
    {
        return StatusCode(StatusCodes.Status405MethodNotAllowed);
    }

    private bool IsViewerAllowed()
    {
        return _demoOptions.Value.AnonymousViewer.Enabled;
    }

    private static DiffItemResponse MapDiffItem(DiffItem item)
    {
        return new DiffItemResponse
        {
            Section = item.Section,
            Key = item.Key,
            DiffKind = item.DiffKind,
            BeforeValue = item.BeforeValue,
            AfterValue = item.AfterValue,
            Notes = item.Notes
        };
    }

    private static ManifestComparisonResponse MapManifestResponse(ManifestComparisonResult result)
    {
        return new ManifestComparisonResponse
        {
            LeftManifestId = result.LeftManifestId,
            RightManifestId = result.RightManifestId,
            LeftManifestHash = result.LeftManifestHash,
            RightManifestHash = result.RightManifestHash,
            AddedCount = result.AddedCount,
            RemovedCount = result.RemovedCount,
            ChangedCount = result.ChangedCount,
            Diffs = result.Diffs.Select(MapDiffItem).ToList(),
            DiffCount = result.Diffs.Count
        };
    }
}
