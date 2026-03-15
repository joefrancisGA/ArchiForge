using ArchiForge.Application.Diffs;

namespace ArchiForge.Application.Determinism;

public sealed class DeterminismCheckService : IDeterminismCheckService
{
    private readonly IReplayRunService _replayRunService;
    private readonly IAgentResultDiffService _agentResultDiffService;
    private readonly IManifestDiffService _manifestDiffService;

    public DeterminismCheckService(
        IReplayRunService replayRunService,
        IAgentResultDiffService agentResultDiffService,
        IManifestDiffService manifestDiffService)
    {
        _replayRunService = replayRunService;
        _agentResultDiffService = agentResultDiffService;
        _manifestDiffService = manifestDiffService;
    }

    public async Task<DeterminismCheckResult> RunAsync(
        DeterminismCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RunId))
            throw new InvalidOperationException("RunId is required.");

        if (request.Iterations < 2)
            throw new InvalidOperationException("Iterations must be at least 2.");

        var output = new DeterminismCheckResult
        {
            SourceRunId = request.RunId,
            Iterations = request.Iterations,
            ExecutionMode = request.ExecutionMode
        };

        var baseline = await _replayRunService.ReplayAsync(
            request.RunId,
            request.ExecutionMode,
            commitReplay: request.CommitReplays,
            manifestVersionOverride: request.CommitReplays ? "determinism-baseline" : null,
            cancellationToken: cancellationToken);

        output.BaselineReplayRunId = baseline.ReplayRunId;

        for (var i = 1; i <= request.Iterations; i++)
        {
            var replay = await _replayRunService.ReplayAsync(
                request.RunId,
                request.ExecutionMode,
                commitReplay: request.CommitReplays,
                manifestVersionOverride: request.CommitReplays ? $"determinism-{i}" : null,
                cancellationToken: cancellationToken);

            var agentDiff = _agentResultDiffService.Compare(
                baseline.ReplayRunId,
                baseline.Results,
                replay.ReplayRunId,
                replay.Results);

            var hasAgentDrift = agentDiff.AgentDeltas.Any(d =>
                d.AddedClaims.Count > 0 ||
                d.RemovedClaims.Count > 0 ||
                d.AddedEvidenceRefs.Count > 0 ||
                d.RemovedEvidenceRefs.Count > 0 ||
                d.AddedFindings.Count > 0 ||
                d.RemovedFindings.Count > 0 ||
                d.AddedRequiredControls.Count > 0 ||
                d.RemovedRequiredControls.Count > 0 ||
                d.AddedWarnings.Count > 0 ||
                d.RemovedWarnings.Count > 0 ||
                d.LeftConfidence != d.RightConfidence);

            var iteration = new DeterminismIterationResult
            {
                IterationNumber = i,
                ReplayRunId = replay.ReplayRunId,
                MatchesBaselineAgentResults = !hasAgentDrift
            };

            if (hasAgentDrift)
            {
                iteration.AgentDriftWarnings.Add("Agent results differ from baseline replay.");
            }

            if (baseline.Manifest is not null && replay.Manifest is not null)
            {
                var manifestDiff = _manifestDiffService.Compare(baseline.Manifest, replay.Manifest);

                var hasManifestDrift =
                    manifestDiff.AddedServices.Count > 0 ||
                    manifestDiff.RemovedServices.Count > 0 ||
                    manifestDiff.AddedDatastores.Count > 0 ||
                    manifestDiff.RemovedDatastores.Count > 0 ||
                    manifestDiff.AddedRequiredControls.Count > 0 ||
                    manifestDiff.RemovedRequiredControls.Count > 0 ||
                    manifestDiff.AddedRelationships.Count > 0 ||
                    manifestDiff.RemovedRelationships.Count > 0;

                iteration.MatchesBaselineManifest = !hasManifestDrift;

                if (hasManifestDrift)
                {
                    iteration.ManifestDriftWarnings.Add("Manifest differs from baseline replay.");
                }
            }
            else
            {
                iteration.MatchesBaselineManifest = true;
            }

            output.IterationResults.Add(iteration);
        }

        output.IsDeterministic = output.IterationResults.All(x =>
            x.MatchesBaselineAgentResults && x.MatchesBaselineManifest);

        if (!output.IsDeterministic)
        {
            output.Warnings.Add("Determinism check detected replay drift.");
        }

        return output;
    }
}
