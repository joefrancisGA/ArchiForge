namespace ArchLucid.Cli.Commands;

/// <summary>
/// Subset of <c>GET /v1/pilots/runs/{runId}/pilot-run-deltas</c> consumed by the smoke runner.
/// Only <c>timeToCommittedManifestTotalSeconds</c> is required to print a meaningful PASS line.
/// </summary>
internal sealed class TrialSmokePilotRunDeltasShape
{
    public double? TimeToCommittedManifestTotalSeconds { get; init; }
}
