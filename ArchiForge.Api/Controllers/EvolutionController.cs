using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Learning;
using ArchiForge.Api.Models.Evolution;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services.Evolution;
using ArchiForge.Contracts.Evolution;
using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Evolution;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

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
    /// <summary>Creates a reviewable candidate from a persisted 59R improvement plan (copies a JSON snapshot).</summary>
    [HttpPost("candidates/from-plan/{planId:guid}")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
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
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
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

    /// <summary>Lists recent candidate change sets for the current scope.</summary>
    [HttpGet("candidates")]
    [Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
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
    [Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
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
