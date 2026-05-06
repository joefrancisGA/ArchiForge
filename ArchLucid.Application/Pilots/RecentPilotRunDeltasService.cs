using ArchLucid.Application.Bootstrap;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Pilots;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;
/// <inheritdoc cref = "IRecentPilotRunDeltasService"/>
/// <remarks>
///     Filters <see cref = "IRunDetailQueryService.ListRunSummariesAsync"/> to runs that already carry a
///     <c>CurrentManifestVersion</c> (i.e. committed) so the panel never advertises an aggregate that mixes
///     in-flight and committed runs. Compute cost is bounded: <see cref = "IRecentPilotRunDeltasService.MaxCount"/>
///     × one <see cref = "IPilotRunDeltaComputer.ComputeAsync"/> per run.
/// </remarks>
public sealed class RecentPilotRunDeltasService(IRunDetailQueryService runDetailQueryService, IPilotRunDeltaComputer pilotRunDeltaComputer, ILogger<RecentPilotRunDeltasService> logger) : IRecentPilotRunDeltasService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runDetailQueryService, pilotRunDeltaComputer, logger);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.IRunDetailQueryService runDetailQueryService, ArchLucid.Application.Pilots.IPilotRunDeltaComputer pilotRunDeltaComputer, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.Pilots.RecentPilotRunDeltasService> logger)
    {
        ArgumentNullException.ThrowIfNull(runDetailQueryService);
        ArgumentNullException.ThrowIfNull(pilotRunDeltaComputer);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    private readonly ILogger<RecentPilotRunDeltasService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPilotRunDeltaComputer _pilotRunDeltaComputer = pilotRunDeltaComputer ?? throw new ArgumentNullException(nameof(pilotRunDeltaComputer));
    private readonly IRunDetailQueryService _runDetailQueryService = runDetailQueryService ?? throw new ArgumentNullException(nameof(runDetailQueryService));
    /// <inheritdoc/>
    public async Task<RecentPilotRunDeltasResponse> GetRecentDeltasAsync(int count, CancellationToken cancellationToken = default)
    {
        int requested = Math.Clamp(count, IRecentPilotRunDeltasService.MinCount, IRecentPilotRunDeltasService.MaxCount);
        IReadOnlyList<RunSummary> all = await _runDetailQueryService.ListRunSummariesAsync(cancellationToken);
        List<RunSummary> committed = all.Where(IsCommitted).OrderByDescending(r => r.CompletedUtc ?? r.CreatedUtc).Take(requested).ToList();
        List<RecentPilotRunDeltaSummaryResponse> rows = [];
        foreach (RunSummary summary in committed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RecentPilotRunDeltaSummaryResponse? row = await TryProjectAsync(summary, cancellationToken);
            if (row is not null)
                rows.Add(row);
        }

        double? medianFindings = ComputeMedian(rows.Select(r => (double)r.TotalFindings));
        double? medianSeconds = ComputeMedian(rows.Where(r => r.TimeToCommittedManifestTotalSeconds is { } s && double.IsFinite(s) && s >= 0).Select(r => r.TimeToCommittedManifestTotalSeconds!.Value));
        return new RecentPilotRunDeltasResponse
        {
            Items = rows,
            RequestedCount = requested,
            ReturnedCount = rows.Count,
            MedianTotalFindings = medianFindings,
            MedianTimeToCommittedManifestTotalSeconds = medianSeconds
        };
    }

    private static bool IsCommitted(RunSummary r)
    {
        return !string.IsNullOrWhiteSpace(r.CurrentManifestVersion);
    }

    private async Task<RecentPilotRunDeltaSummaryResponse?> TryProjectAsync(RunSummary summary, CancellationToken cancellationToken)
    {
        try
        {
            ArchitectureRunDetail? detail = await _runDetailQueryService.GetRunDetailAsync(summary.RunId, cancellationToken);
            if (detail is null)
                return null;
            PilotRunDeltas deltas = await _pilotRunDeltaComputer.ComputeAsync(detail, cancellationToken);
            int totalFindings = deltas.FindingsBySeverity.Sum(p => p.Value);
            bool isDemo = deltas.IsDemoTenant || ContosoRetailDemoIdentifiers.IsDemoRunId(summary.RunId) || ContosoRetailDemoIdentifiers.IsDemoRequestId(summary.RequestId);
            return new RecentPilotRunDeltaSummaryResponse
            {
                RunId = summary.RunId,
                RequestId = summary.RequestId,
                RunCreatedUtc = deltas.RunCreatedUtc,
                ManifestCommittedUtc = deltas.ManifestCommittedUtc,
                TimeToCommittedManifestTotalSeconds = deltas.TimeToCommittedManifest?.TotalSeconds,
                TotalFindings = totalFindings,
                TopFindingSeverity = deltas.TopFindingSeverity,
                IsDemoTenant = isDemo
            };
        }
        catch (Exception ex)when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Recent pilot run deltas: skipping run {RunId} because the per-run delta projection failed.", summary.RunId);
            return null;
        }
    }

    /// <summary>
    ///     Population median (linear-interpolation midpoint for even sample size) — keeps the headline stable
    ///     when one outlier run dominates a small window. Returns <see langword="null"/> on an empty source.
    /// </summary>
    internal static double? ComputeMedian(IEnumerable<double> values)
    {
        double[] sorted = values.OrderBy(v => v).ToArray();
        if (sorted.Length == 0)
            return null;
        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}