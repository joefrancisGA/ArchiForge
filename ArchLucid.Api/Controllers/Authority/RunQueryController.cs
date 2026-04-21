using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Architecture;
using ArchLucid.Application.Explanation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Authority;

/// <summary>
/// Read-only HTTP API for architecture runs: detail, provenance, decisions, evidence, traces, and list.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class RunQueryController(
    IRunDetailQueryService runDetailQueryService,
    IArchitectureRunProvenanceService architectureRunProvenanceService,
    IRunRepository authorityRunRepository,
    IDecisionNodeRepository decisionNodeRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IAgentExecutionTraceRepository agentExecutionTraceRepository,
    IFindingEvidenceChainService findingEvidenceChainService,
    IScopeContextProvider scopeContextProvider) : ControllerBase
{
    /// <summary>
    /// Returns the canonical run aggregate (tasks, results, manifest, decision traces) for <paramref name="runId"/>.
    /// </summary>
    [HttpGet("run/{runId}")]
    [ProducesResponseType(typeof(RunDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        if (!string.IsNullOrWhiteSpace(detail.Run.CurrentManifestVersion) && detail.Manifest is null)
            return this.NotFoundProblem(
                $"Manifest referenced by run '{runId}' could not be found.",
                ProblemTypes.ResourceNotFound);


        RunDetailsResponse response = RunResponseMapper.ToRunDetailsResponse(
            detail.Run,
            detail.Tasks,
            detail.Results,
            detail.Manifest,
            detail.DecisionTraces);

        return Ok(response);
    }

    /// <summary>
    /// Returns the coordinator linkage graph (request, tasks, results, findings, manifest, traces, decisions) and a sorted trace timeline.
    /// </summary>
    [HttpGet("runs/{runId}/provenance")]
    [ProducesResponseType(typeof(ArchitectureRunProvenanceGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArchitectureRunProvenance(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRunProvenanceGraph? graph = await architectureRunProvenanceService
            .GetProvenanceAsync(runId, cancellationToken);

        if (graph is null)
            return this.NotFoundProblem(
                $"Run '{runId}' was not found, or its manifest reference is broken.",
                ProblemTypes.RunNotFound);


        return Ok(graph);
    }

    /// <summary>
    /// Returns decision-tree nodes materialized for <paramref name="runId"/> after commit (empty before commit yields 404).
    /// </summary>
    [HttpGet("run/{runId}/decisions")]
    [ProducesResponseType(typeof(DecisionNodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunDecisions(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        IReadOnlyList<DecisionNode> decisions = await decisionNodeRepository.GetByRunIdAsync(runId, cancellationToken);

        if (decisions.Count == 0)
            return this.NotFoundProblem(
                $"No decisions found for run '{runId}'. Decisions are available after the run has been committed.",
                ProblemTypes.ResourceNotFound);


        return Ok(new DecisionNodeResponse
        {
            Decisions = decisions.ToList()
        });
    }

    /// <summary>
    /// Returns the hydrated <see cref="AgentEvidencePackage"/> used when agents ran for <paramref name="runId"/>.
    /// </summary>
    [HttpGet("run/{runId}/evidence")]
    [ProducesResponseType(typeof(AgentEvidencePackageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunEvidence(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        AgentEvidencePackage? evidence = await agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken);
        if (evidence is null)
            return this.NotFoundProblem($"Evidence for run '{runId}' was not found.", ProblemTypes.ResourceNotFound);


        return Ok(new AgentEvidencePackageResponse
        {
            Evidence = evidence
        });
    }

    /// <summary>
    /// Returns a page of <see cref="AgentExecutionTrace"/> rows for <paramref name="runId"/> (prompt/response audit trail).
    /// </summary>
    [HttpGet("run/{runId}/traces")]
    [ProducesResponseType(typeof(AgentExecutionTraceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunTraces(
        [FromRoute] string runId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            return this.BadRequestProblem("pageNumber must be at least 1.", ProblemTypes.ValidationFailed);


        if (pageSize < 1 || pageSize > PagingParameters.MaxPageSize)
            return this.BadRequestProblem(
                $"pageSize must be between 1 and {PagingParameters.MaxPageSize}.",
                ProblemTypes.ValidationFailed);


        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        PagingParameters paging = new()
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        (int skip, int take) = paging.Normalize();

        (IReadOnlyList<AgentExecutionTrace> pagedTraces, int totalCount) = await agentExecutionTraceRepository.GetPagedByRunIdAsync(
            runId,
            offset: skip,
            limit: take,
            cancellationToken: cancellationToken);

        return Ok(new AgentExecutionTraceResponse
        {
            Traces = pagedTraces.ToList(),
            TotalCount = totalCount,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        });
    }

    /// <summary>
    /// Lists recent runs visible in the current scope (summary rows for dashboards and pickers).
    /// </summary>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(List<RunListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuns(CancellationToken cancellationToken)
    {
        IReadOnlyList<RunSummary> summaries = await runDetailQueryService.ListRunSummariesAsync(cancellationToken);

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

    /// <summary>
    /// Returns persisted artifact pointers for one finding (manifest snapshot ids, graph nodes, agent trace ids).
    /// </summary>
    [HttpGet("run/{runId}/findings/{findingId}/evidence-chain")]
    [ProducesResponseType(typeof(FindingEvidenceChainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFindingEvidenceChain(
        [FromRoute] string runId,
        [FromRoute] string findingId,
        CancellationToken cancellationToken)
    {
        FindingEvidenceChainResponse? chain =
            await findingEvidenceChainService.BuildAsync(runId, findingId, cancellationToken);

        if (chain is null)
            return this.NotFoundProblem(
                $"Evidence chain is not available for run '{runId}' and finding '{findingId}'.",
                ProblemTypes.ResourceNotFound);


        return Ok(chain);
    }

    private async Task<bool> AuthorityRunExistsInScopeAsync(string runId, CancellationToken cancellationToken)
    {
        if (!TryParseRunId(runId, out Guid runGuid))
            return false;


        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        return await authorityRunRepository.GetByIdAsync(scope, runGuid, cancellationToken) is not null;
    }

    private static bool TryParseRunId(string runId, out Guid runGuid)
    {
        if (Guid.TryParseExact(runId, "N", out runGuid))
            return true;


        return Guid.TryParse(runId, out runGuid);
    }
}
