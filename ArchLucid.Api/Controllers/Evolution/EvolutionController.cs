using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Core.Authorization;
using ArchLucid.Api.Learning;
using ArchLucid.Api.Models.Evolution;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Api.ProductLearning;
using ArchLucid.Api.Services.Evolution;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Coordination.Evolution;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// 60R controlled evolution: candidate change sets from 59R plans and read-only shadow evaluation (simulation only).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/evolution")]
[EnableRateLimiting("fixed")]
public sealed class EvolutionController(
    IEvolutionSimulationService evolutionSimulationService,
    IEvolutionCandidateChangeSetRepository candidateRepository,
    IEvolutionSimulationRunRepository simulationRunRepository,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    private static readonly JsonSerializerOptions SimulationReportFileJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>Creates a reviewable candidate from a persisted 59R improvement plan (copies a JSON snapshot).</summary>
    [HttpPost("candidates/from-plan/{planId:guid}")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(EvolutionCandidateChangeSetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCandidateFromPlan(
        Guid planId,
        CancellationToken cancellationToken)
    {
        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());

        try
        {
            EvolutionCandidateChangeSetRecord record =
                await evolutionSimulationService.CreateCandidateFromImprovementPlanAsync(
                    planId,
                    scope,
                    createdByUserId: null,
                    cancellationToken);

            return Ok(record.ToResponse());
        }
        catch (EvolutionResourceNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ex.ProblemTypeUri);
        }
    }

    /// <summary>Runs read-only architecture analysis for each baseline run linked on the source plan (persists shadow rows; no commits/replays).</summary>
    [HttpPost("candidates/{candidateId:guid}/shadow-evaluate")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(EvolutionShadowEvaluateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShadowEvaluate(Guid candidateId, CancellationToken cancellationToken)
    {
        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());

        try
        {
            IReadOnlyList<EvolutionSimulationRunRecord> runs =
                await evolutionSimulationService.RunShadowEvaluationAsync(candidateId, scope, cancellationToken);

            EvolutionShadowEvaluateResponse body = new()
            {
                SimulationRuns = runs.Select(static r => r.ToResponse()).ToList(),
            };

            return Ok(body);
        }
        catch (EvolutionResourceNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ex.ProblemTypeUri);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
    }

    /// <summary>
    /// Re-runs simulation for the candidate (replaces prior simulation rows), persists 60R-v2 outcomes with evaluation scores.
    /// </summary>
    [HttpPost("simulate/{candidateId:guid}")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(EvolutionSimulateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Simulate(Guid candidateId, CancellationToken cancellationToken)
    {
        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());

        try
        {
            IReadOnlyList<EvolutionSimulationRunRecord> runs =
                await evolutionSimulationService.SimulateCandidateWithEvaluationAsync(
                    candidateId,
                    scope,
                    cancellationToken);

            EvolutionCandidateChangeSetRecord? candidate =
                await candidateRepository.GetByIdAsync(candidateId, scope, cancellationToken);

            if (candidate is null)
            {
                return this.NotFoundProblem(
                    $"Candidate change set '{candidateId}' was not found in the current scope.",
                    ProblemTypes.EvolutionCandidateChangeSetNotFound);
            }

            EvolutionSimulateResponse body = new()
            {
                Candidate = candidate.ToResponse(),
                SimulationRuns = runs.Select(EvolutionOutcomeParser.ToRunWithEvaluation).ToList(),
            };

            return Ok(body);
        }
        catch (EvolutionResourceNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ex.ProblemTypeUri);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
    }

    /// <summary>Loads candidate, plan snapshot, and simulation runs with parsed evaluation scores.</summary>
    [HttpGet("results/{candidateId:guid}")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(EvolutionResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResults(Guid candidateId, CancellationToken cancellationToken)
    {
        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());

        EvolutionCandidateChangeSetRecord? row =
            await candidateRepository.GetByIdAsync(candidateId, scope, cancellationToken);

        if (row is null)
        {
            return this.NotFoundProblem(
                $"Candidate change set '{candidateId}' was not found in the current scope.",
                ProblemTypes.EvolutionCandidateChangeSetNotFound);
        }

        IReadOnlyList<EvolutionSimulationRunRecord> sims =
            await simulationRunRepository.ListByCandidateAsync(candidateId, cancellationToken);

        EvolutionResultsResponse body = new()
        {
            Candidate = row.ToResponse(),
            PlanSnapshotJson = row.PlanSnapshotJson,
            SimulationRuns = sims.Select(EvolutionOutcomeParser.ToRunWithEvaluation).ToList(),
        };

        return Ok(body);
    }

    /// <summary>Downloads a Markdown or JSON simulation report (change set, plan snapshot, runs, scores, diff summary).</summary>
    [HttpGet("results/{candidateId:guid}/export")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportResults(
        Guid candidateId,
        [FromQuery] string? format,
        CancellationToken cancellationToken)
    {
        if (!ProductLearningQueryParser.TryParseReportFormat(format, out string formatNorm, out string? formatError))
        {
            return this.BadRequestProblem(formatError!, ProblemTypes.ValidationFailed);
        }

        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());

        EvolutionCandidateChangeSetRecord? row =
            await candidateRepository.GetByIdAsync(candidateId, scope, cancellationToken);

        if (row is null)
        {
            return this.NotFoundProblem(
                $"Candidate change set '{candidateId}' was not found in the current scope.",
                ProblemTypes.EvolutionCandidateChangeSetNotFound);
        }

        IReadOnlyList<EvolutionSimulationRunRecord> sims =
            await simulationRunRepository.ListByCandidateAsync(candidateId, cancellationToken);

        EvolutionSimulationReportDocument document =
            EvolutionSimulationReportBuilder.Build(row, sims, DateTime.UtcNow);

        string fileStem = $"evolution-simulation-report-{candidateId:N}";

        if (string.Equals(formatNorm, "json", StringComparison.Ordinal))
        {
            string json = JsonSerializer.Serialize(document, SimulationReportFileJsonOptions);

            return ApiFileResults.RangeText(Request, json, "application/json", $"{fileStem}.json");
        }

        string markdown = EvolutionSimulationReportMarkdownFormatter.Format(document);

        return ApiFileResults.RangeText(Request, markdown, "text/markdown", $"{fileStem}.md");
    }

    /// <summary>Lists recent candidate change sets for the current scope.</summary>
    [HttpGet("candidates")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(EvolutionCandidateChangeSetListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListCandidates([FromQuery] string? max, CancellationToken cancellationToken)
    {
        if (!LearningPlanningQueryParser.TryParseMaxItems(max, "max", out int take, out string? maxError))
        {
            return this.BadRequestProblem(maxError!, ProblemTypes.ValidationFailed);
        }

        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());

        IReadOnlyList<EvolutionCandidateChangeSetRecord> rows =
            await candidateRepository.ListAsync(scope, take, cancellationToken);

        EvolutionCandidateChangeSetListResponse body = new()
        {
            Candidates = rows.Select(static r => r.ToResponse()).ToList(),
        };

        return Ok(body);
    }

    /// <summary>Loads one candidate, its snapshot JSON, and persisted simulation rows.</summary>
    [HttpGet("candidates/{candidateId:guid}")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(EvolutionCandidateDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCandidate(Guid candidateId, CancellationToken cancellationToken)
    {
        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());

        EvolutionCandidateChangeSetRecord? row =
            await candidateRepository.GetByIdAsync(candidateId, scope, cancellationToken);

        if (row is null)
        {
            return this.NotFoundProblem(
                $"Candidate change set '{candidateId}' was not found in the current scope.",
                ProblemTypes.EvolutionCandidateChangeSetNotFound);
        }

        IReadOnlyList<EvolutionSimulationRunRecord> sims =
            await simulationRunRepository.ListByCandidateAsync(candidateId, cancellationToken);

        EvolutionCandidateDetailResponse body = new()
        {
            Candidate = row.ToResponse(),
            PlanSnapshotJson = row.PlanSnapshotJson,
            SimulationRuns = sims.Select(static s => s.ToResponse()).ToList(),
        };

        return Ok(body);
    }

    private static ProductLearningScope ToProductLearningScope(ScopeContext scopeContext)
    {
        ArgumentNullException.ThrowIfNull(scopeContext);

        return new ProductLearningScope
        {
            TenantId = scopeContext.TenantId,
            WorkspaceId = scopeContext.WorkspaceId,
            ProjectId = scopeContext.ProjectId,
        };
    }
}
