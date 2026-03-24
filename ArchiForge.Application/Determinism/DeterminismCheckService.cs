using ArchiForge.Application.Diffs;

namespace ArchiForge.Application.Determinism;

/// <summary>
/// Checks the determinism of an architecture run by replaying it multiple times and comparing
/// agent results and manifest output across iterations. Returns a <see cref="DeterminismCheckResult"/>
/// indicating whether the run produces consistent output.
/// </summary>
public sealed class DeterminismCheckService(
    IReplayRunService replayRunService,
    IAgentResultDiffService agentResultDiffService,
    IManifestDiffService manifestDiffService)
    : IDeterminismCheckService
{
    /// <inheritdoc />
    public async Task<DeterminismCheckResult> RunAsync(
        DeterminismCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RunId);

        if (request.Iterations < 2)
            throw new ArgumentOutOfRangeException(nameof(request), "Iterations must be at least 2.");

        var output = new DeterminismCheckResult
        {
            SourceRunId = request.RunId,
            Iterations = request.Iterations,
            ExecutionMode = request.ExecutionMode
        };

        var baseline = await replayRunService.ReplayAsync(
            request.RunId,
            request.ExecutionMode,
            commitReplay: request.CommitReplays,
            manifestVersionOverride: request.CommitReplays ? DeterminismVersionConstants.BaselineVersion : null,
            cancellationToken: cancellationToken);

        output.BaselineReplayRunId = baseline.ReplayRunId;

        for (var i = 1; i <= request.Iterations; i++)
        {
            var replay = await replayRunService.ReplayAsync(
                request.RunId,
                request.ExecutionMode,
                commitReplay: request.CommitReplays,
                manifestVersionOverride: request.CommitReplays ? DeterminismVersionConstants.IterationVersion(i) : null,
                cancellationToken: cancellationToken);

            var agentDiff = agentResultDiffService.Compare(
                baseline.ReplayRunId,
                baseline.Results,
                replay.ReplayRunId,
                replay.Results);

            var hasAgentDrift = HasAgentDrift(agentDiff);

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
                var manifestDiff = manifestDiffService.Compare(baseline.Manifest, replay.Manifest);
                var hasManifestDrift = HasManifestDrift(manifestDiff);
                iteration.MatchesBaselineManifest = !hasManifestDrift;

                if (hasManifestDrift)
                {
                    iteration.ManifestDriftWarnings.Add("Manifest differs from baseline replay.");
                }
            }
            else if (baseline.Manifest is null && replay.Manifest is null)
            {
                // Neither run produced a manifest; treat as matching.
                iteration.MatchesBaselineManifest = true;
            }
            else
            {
                // One run produced a manifest and the other did not — this is drift.
                iteration.MatchesBaselineManifest = false;
                iteration.ManifestDriftWarnings.Add(
                    "Manifest presence is asymmetric: one replay produced a manifest while the other did not.");
            }

            output.IterationResults.Add(iteration);
        }

        output.IsDeterministic = output.IterationResults.All(x =>
            x is { MatchesBaselineAgentResults: true, MatchesBaselineManifest: true });

        if (!output.IsDeterministic)
        {
            output.Warnings.Add("Determinism check detected replay drift.");
        }

        return output;
    }

    /// <summary>
    /// Returns <c>true</c> when any agent delta in <paramref name="diff"/> contains at least one
    /// added or removed claim, evidence ref, finding, required control, warning, or a confidence change.
    /// </summary>
    private static bool HasAgentDrift(AgentResultDiffResult diff) =>
        diff.AgentDeltas.Any(d =>
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
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            d.LeftConfidence != d.RightConfidence);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="diff"/> reports any added or removed services,
    /// datastores, required controls, or relationships.
    /// </summary>
    private static bool HasManifestDrift(ManifestDiffResult diff) =>
        diff.AddedServices.Count > 0 ||
        diff.RemovedServices.Count > 0 ||
        diff.AddedDatastores.Count > 0 ||
        diff.RemovedDatastores.Count > 0 ||
        diff.AddedRequiredControls.Count > 0 ||
        diff.RemovedRequiredControls.Count > 0 ||
        diff.AddedRelationships.Count > 0 ||
        diff.RemovedRelationships.Count > 0;
}
