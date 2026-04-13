using ArchLucid.Api.ProblemDetails;
using ArchLucid.Contracts.Agents;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers;

public sealed partial class RunsController
{
    /// <summary>
    /// On-demand structural evaluation of <see cref="AgentExecutionTrace.ParsedResultJson"/> for traces in the run (no metrics).
    /// </summary>
    [HttpGet("run/{runId}/agent-evaluation")]
    [ProducesResponseType(typeof(AgentOutputEvaluationSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunAgentEvaluation(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))

            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        IReadOnlyList<AgentExecutionTrace> traces = await agentExecutionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

        List<AgentOutputEvaluationScore> scores = new(capacity: traces.Count);
        int skipped = 0;
        List<double> ratiosForAverage = new();

        foreach (AgentExecutionTrace trace in traces)
        {
            if (!trace.ParseSucceeded || string.IsNullOrEmpty(trace.ParsedResultJson))
            {
                skipped++;
                continue;
            }

            AgentOutputEvaluationScore score = agentOutputEvaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);
            scores.Add(score);

            if (!score.IsJsonParseFailure)
            {
                ratiosForAverage.Add(score.StructuralCompletenessRatio);
            }
        }

        double? average = ratiosForAverage.Count == 0
            ? null
            : ratiosForAverage.Average();

        AgentOutputEvaluationSummary summary = new()
        {
            RunId = runId,
            EvaluatedAtUtc = DateTime.UtcNow,
            Scores = scores,
            TracesSkippedCount = skipped,
            AverageStructuralCompletenessRatio = average,
        };

        return Ok(summary);
    }
}
