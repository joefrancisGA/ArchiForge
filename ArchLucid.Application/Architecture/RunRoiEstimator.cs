using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Architecture;

/// <inheritdoc cref="IRunRoiEstimator" />
public sealed class RunRoiEstimator(IOptions<RunRoiEstimatorOptions>? optionsMonitor)
    : IRunRoiEstimator
{
    private readonly RunRoiEstimatorOptions _opts =
        optionsMonitor?.Value ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <inheritdoc />
    public RunRoiScorecardDto Estimate(ArchitectureRunDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        string runId = detail.Run.RunId.Trim();
        int findingTotal = detail.Results.Sum(static r => r.Findings.Count);
        int criticalFindings = detail.Results.Sum(r => r.Findings.Count(f => f.Severity == Contracts.Findings.FindingSeverity.Critical));
        int errorFindings = detail.Results.Sum(r => r.Findings.Count(f => f.Severity == Contracts.Findings.FindingSeverity.Error));
        int warningFindings = detail.Results.Sum(r => r.Findings.Count(f => f.Severity == Contracts.Findings.FindingSeverity.Warning));
        int infoFindings = detail.Results.Sum(r => r.Findings.Count(f => f.Severity == Contracts.Findings.FindingSeverity.Info));
        int resultCount = detail.Results.Count;
        int manifestElements =
            detail.Manifest is null ? 0 : ManifestElementCount(detail.Manifest);
        int traceCount = detail.DecisionTraces.Count;

        double findingHours = 
            criticalFindings * _opts.HoursPerCriticalFinding +
            errorFindings * _opts.HoursPerErrorFinding +
            warningFindings * _opts.HoursPerWarningFinding +
            infoFindings * _opts.HoursPerInfoFinding;

        double hours =
            findingHours +
            manifestElements * _opts.HoursPerManifestModeledElement +
            traceCount * _opts.HoursPerDecisionTrace +
            resultCount * _opts.HoursPerCompletedAgentResult;

        hours = Math.Round(hours, 2, MidpointRounding.AwayFromZero);

        return new RunRoiScorecardDto
        {
            RunId = runId,
            AgentFindingTotalCount = findingTotal,
            CompletedAgentResultCount = resultCount,
            ManifestModeledElementApproxCount = manifestElements,
            DecisionTraceCount = traceCount,
            EstimatedManualHoursSaved = hours,
            EstimatedUtc = DateTime.UtcNow,
            ComputationNotes =
                "Directional analyst-hour estimate from committed run aggregates; not financial advice. "
                + "Multipliers configured under Architecture:RunRoiEstimator."
        };
    }

    private static int ManifestElementCount(GoldenManifest m)
    {
        return m.Services.Count + m.Datastores.Count + m.Relationships.Count;
    }
}
