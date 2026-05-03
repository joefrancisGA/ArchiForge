using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Requests;
using ArchLucid.Persistence.Models;

using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Application.Runs.Telemetry;

/// <summary>Scalar metrics written to <c>dbo.RunTelemetry</c> at successful authority commit.</summary>
public sealed record CommitRunTelemetryMetrics(
    long RequestDurationMs,
    long AgentExecutionDurationMs,
    long ManualReviewDurationMs,
    decimal EstimatedHoursSaved)
{
    /// <summary>
    ///     Derives durations from persisted run, evidence package, and agent results.
    ///     <see cref="AgentEvidencePackage.CreatedUtc" /> approximates execute-phase start; agent result
    ///     <c>CreatedUtc</c> values bound the agent batch; commit time closes the pre-commit window.
    /// </summary>
    public static CommitRunTelemetryMetrics FromCommitContext(
        RunRecord runHeader,
        AgentEvidencePackage evidence,
        IReadOnlyList<AgentResult> agentResults,
        DateTime commitUtcUtc,
        Dm.ManifestDocument persistedManifest)
    {
        ArgumentNullException.ThrowIfNull(runHeader);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(agentResults);
        ArgumentNullException.ThrowIfNull(persistedManifest);

        DateTime runCreated = NormalizeToUtc(runHeader.CreatedUtc);
        DateTime evidenceCreated = NormalizeToUtc(evidence.CreatedUtc);
        DateTime commitUtc = NormalizeToUtc(commitUtcUtc);

        long requestMs = MillisecondsBetween(runCreated, evidenceCreated);

        DateTime agentLo =
            agentResults.Count > 0
                ? agentResults.Min(static r => NormalizeToUtc(r.CreatedUtc))
                : evidenceCreated;

        DateTime agentHi =
            agentResults.Count > 0
                ? agentResults.Max(static r => NormalizeToUtc(r.CreatedUtc))
                : evidenceCreated;

        long agentMs = MillisecondsBetween(agentLo, agentHi);

        DateTime reviewStart = agentHi > evidenceCreated ? agentHi : evidenceCreated;

        long manualMs = MillisecondsBetween(reviewStart, commitUtc);

        int findingCount = agentResults.Sum(static r => r.Findings.Count);
        int warningCount = persistedManifest.Warnings.Count;

        decimal estimatedHoursSaved =
            Math.Round((decimal)(findingCount * 0.25 + warningCount * 0.5), 2, MidpointRounding.AwayFromZero);

        if (estimatedHoursSaved < 0.25m)
            estimatedHoursSaved = 0.25m;

        if (estimatedHoursSaved > 80m)
            estimatedHoursSaved = 80m;

        return new CommitRunTelemetryMetrics(requestMs, agentMs, manualMs, estimatedHoursSaved);
    }

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);

    private static long MillisecondsBetween(DateTime earlier, DateTime later)
    {
        if (later < earlier)
            return 0L;

        double ms = (later - earlier).TotalMilliseconds;

        return ms > long.MaxValue ? long.MaxValue : (long)Math.Round(ms);
    }
}
