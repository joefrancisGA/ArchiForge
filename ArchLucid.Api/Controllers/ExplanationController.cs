using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Comparison;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// LLM explanations for a single run (with optional provenance) and for manifest deltas between two runs.
/// </summary>
/// <remarks>Routes under <c>api/explain</c>; uses <see cref="IExplanationService"/> and <see cref="IComparisonService"/> for compare narrative.</remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/explain")]
[EnableRateLimiting("fixed")]
public sealed class ExplanationController(
    IAuthorityQueryService query,
    IComparisonService comparison,
    IExplanationService explanation,
    IRunExplanationSummaryService runExplanationSummary,
    IProvenanceSnapshotRepository provenanceRepo,
    IScopeContextProvider scopeProvider,
    ILogger<ExplanationController> logger)
    : ControllerBase
{
    /// <summary>Stakeholder explanation for one run�s golden manifest, optionally enriched with stored provenance graph JSON.</summary>
    /// <param name="runId">Run to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ExplanationResult"/> JSON, or 404 when the run or manifest is missing in scope.</returns>
    [HttpGet("runs/{runId:guid}/explain")]
    [ProducesResponseType(typeof(ExplanationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExplainRun(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? detail = await query.GetRunDetailAsync(scope, runId, ct);
        if (detail?.GoldenManifest is null)
            return this.NotFoundProblem(
                $"Run '{runId}' was not found or has no committed manifest in the current scope.",
                ProblemTypes.RunNotFound);

        DecisionProvenanceGraph? graph = null;
        DecisionProvenanceSnapshot? snapshot = await provenanceRepo.GetByRunIdAsync(scope, runId, ct);
        if (snapshot is not null)
        
            try
            {
                graph = ProvenanceGraphSerializer.Deserialize(snapshot.GraphJson);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Provenance graph JSON for run {RunId} is corrupt; explanation will proceed without provenance.", runId);
            }
        

        ExplanationResult result = await explanation.ExplainRunAsync(detail.GoldenManifest, graph, ct);
        return Ok(result);
    }

    /// <summary>Executive rollup: themes, risk posture, counts, and the same explanation payload as granular explain.</summary>
    /// <param name="runId">Run to summarize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="RunExplanationSummary"/> JSON, or 404 when the run or manifest is missing in scope.</returns>
    [HttpGet("runs/{runId:guid}/aggregate")]
    [ProducesResponseType(typeof(RunExplanationSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AggregateRunExplanation(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunExplanationSummary? summary = await runExplanationSummary.GetSummaryAsync(scope, runId, ct);
        if (summary is null)
            return this.NotFoundProblem(
                $"Run '{runId}' was not found or has no committed manifest in the current scope.",
                ProblemTypes.RunNotFound);

        return Ok(summary);
    }

    /// <summary>AI narrative for manifest delta between two runs (base ? target).</summary>
    /// <param name="baseRunId">Baseline run.</param>
    /// <param name="targetRunId">Target run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ComparisonExplanationResult"/> JSON, or 404 when either run lacks a golden manifest in scope.</returns>
    [HttpGet("compare/explain")]
    [ProducesResponseType(typeof(ComparisonExplanationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExplainComparison(
        [FromQuery] Guid baseRunId,
        [FromQuery] Guid targetRunId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? baseRun = await query.GetRunDetailAsync(scope, baseRunId, ct);
        RunDetailDto? targetRun = await query.GetRunDetailAsync(scope, targetRunId, ct);
        if (baseRun?.GoldenManifest is null || targetRun?.GoldenManifest is null)
            return this.NotFoundProblem(
                "One or both runs were not found or have no committed manifest in the current scope.",
                ProblemTypes.RunNotFound);

        ComparisonResult comparison1 = comparison.Compare(baseRun.GoldenManifest, targetRun.GoldenManifest);
        ComparisonExplanationResult result = await explanation.ExplainComparisonAsync(comparison1, ct);
        return Ok(result);
    }
}
