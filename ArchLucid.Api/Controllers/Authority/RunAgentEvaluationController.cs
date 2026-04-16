using ArchLucid.Api.ProblemDetails;
using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.Contracts.Agents;
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
/// On-demand structural and semantic evaluation of agent traces for a run.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class RunAgentEvaluationController(
    IRunRepository authorityRunRepository,
    IAgentExecutionTraceRepository agentExecutionTraceRepository,
    IAgentOutputEvaluator agentOutputEvaluator,
    IAgentOutputSemanticEvaluator agentOutputSemanticEvaluator,
    IScopeContextProvider scopeContextProvider) : ControllerBase
{
    /// <summary>
    /// On-demand structural and semantic evaluation of <see cref="AgentExecutionTrace.ParsedResultJson"/> for traces in the run (no metrics).
    /// </summary>
    [HttpGet("run/{runId}/agent-evaluation")]
    [ProducesResponseType(typeof(AgentOutputEvaluationSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunAgentEvaluation(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        IReadOnlyList<AgentExecutionTrace> traces = await agentExecutionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

        List<AgentOutputEvaluationScore> scores = new(capacity: traces.Count);
        int skipped = 0;
        List<double> ratiosForAverage = new();
        List<double> semanticForAverage = new();

        foreach (AgentExecutionTrace trace in traces)
        {
            if (!trace.ParseSucceeded || string.IsNullOrEmpty(trace.ParsedResultJson))
            {
                skipped++;
                continue;
            }

            AgentOutputEvaluationScore score = agentOutputEvaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);
            score.BlobUploadFailed = trace.BlobUploadFailed;

            if (!score.IsJsonParseFailure)
            {
                score.Semantic = agentOutputSemanticEvaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);
                ratiosForAverage.Add(score.StructuralCompletenessRatio);
                semanticForAverage.Add(score.Semantic.OverallSemanticScore);
            }

            scores.Add(score);
        }

        double? averageStructural = ratiosForAverage.Count == 0
            ? null
            : ratiosForAverage.Average();

        double? averageSemantic = semanticForAverage.Count == 0
            ? null
            : semanticForAverage.Average();

        AgentOutputEvaluationSummary summary = new()
        {
            RunId = runId,
            EvaluatedAtUtc = DateTime.UtcNow,
            Scores = scores,
            TracesSkippedCount = skipped,
            AverageStructuralCompletenessRatio = averageStructural,
            AverageSemanticScore = averageSemantic,
        };

        return Ok(summary);
    }

    private async Task<bool> AuthorityRunExistsInScopeAsync(string runId, CancellationToken cancellationToken)
    {
        if (!TryParseRunId(runId, out Guid runGuid))
        {
            return false;
        }

        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        return await authorityRunRepository.GetByIdAsync(scope, runGuid, cancellationToken) is not null;
    }

    private static bool TryParseRunId(string runId, out Guid runGuid)
    {
        if (Guid.TryParseExact(runId, "N", out runGuid))
        {
            return true;
        }

        return Guid.TryParse(runId, out runGuid);
    }
}
