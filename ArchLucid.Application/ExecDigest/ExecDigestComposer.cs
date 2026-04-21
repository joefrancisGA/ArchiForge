using System.Globalization;
using System.Text;

using ArchLucid.Application.Governance;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Queries;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.ExecDigest;

/// <inheritdoc cref="IExecDigestComposer" />
public sealed class ExecDigestComposer(
    IComplianceDriftTrendService complianceDriftTrendService,
    IAuthorityQueryService authorityQueryService,
    IRunDetailQueryService runDetailQueryService,
    IPilotRunDeltaComputer pilotRunDeltaComputer,
    ILogger<ExecDigestComposer> logger) : IExecDigestComposer
{
    private const int MaxListRuns = 200;

    private const int MaxRunDetailLookups = 40;

    private const int TopRunCount = 3;

    private readonly IComplianceDriftTrendService _complianceDriftTrendService =
        complianceDriftTrendService ?? throw new ArgumentNullException(nameof(complianceDriftTrendService));

    private readonly IAuthorityQueryService _authorityQueryService =
        authorityQueryService ?? throw new ArgumentNullException(nameof(authorityQueryService));

    private readonly IRunDetailQueryService _runDetailQueryService =
        runDetailQueryService ?? throw new ArgumentNullException(nameof(runDetailQueryService));

    private readonly IPilotRunDeltaComputer _pilotRunDeltaComputer =
        pilotRunDeltaComputer ?? throw new ArgumentNullException(nameof(pilotRunDeltaComputer));

    private readonly ILogger<ExecDigestComposer> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<ExecDigestComposition> ComposeAsync(
        Guid tenantId,
        DateTime weekStartUtcInclusive,
        DateTime weekEndUtcExclusive,
        ScopeContext authorityScope,
        string operatorBaseUrl,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        if (authorityScope is null)
            throw new ArgumentNullException(nameof(authorityScope));

        if (weekStartUtcInclusive >= weekEndUtcExclusive)
            throw new ArgumentOutOfRangeException(nameof(weekEndUtcExclusive));

        string baseUrl = NormalizeBaseUrl(operatorBaseUrl);
        string weekLabel = FormatWeekLabel(weekStartUtcInclusive, weekEndUtcExclusive);

        string? complianceMarkdown = await TryBuildComplianceMarkdownAsync(
            tenantId,
            weekStartUtcInclusive,
            weekEndUtcExclusive,
            cancellationToken);

        string dashboardUrl = $"{baseUrl}/runs";

        (int? manifestCount, List<ExecDigestHighlightedRun> highlights, string? latestRunHex, string? findingsDelta) =
            await TryBuildManifestAndFindingSectionsAsync(
                authorityScope,
                weekStartUtcInclusive,
                weekEndUtcExclusive,
                cancellationToken);

        string sponsorUrl = string.IsNullOrWhiteSpace(latestRunHex)
            ? dashboardUrl
            : $"{baseUrl}/runs/{latestRunHex}";

        return new ExecDigestComposition(
            weekLabel,
            complianceMarkdown,
            manifestCount,
            highlights,
            findingsDelta,
            dashboardUrl,
            sponsorUrl,
            latestRunHex);
    }

    private async Task<string?> TryBuildComplianceMarkdownAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            TimeSpan bucket = TimeSpan.FromHours(24);
            IReadOnlyList<ComplianceDriftTrendPoint> points = await _complianceDriftTrendService.GetTrendAsync(
                tenantId,
                fromUtc,
                toUtc,
                bucket,
                cancellationToken);

            if (points is null || points.Count == 0)
                return null;


            StringBuilder sb = new();
            sb.AppendLine("| Day (UTC) | Total changes | Top change types |");
            sb.AppendLine("| --- | ---: | --- |");

            foreach (ComplianceDriftTrendPoint p in points)
            {
                string topTypes = p.ChangesByType.Count == 0
                    ? "—"
                    : string.Join(
                        ", ",
                        p.ChangesByType
                            .OrderByDescending(static kv => kv.Value)
                            .Take(3)
                            .Select(static kv => $"`{kv.Key}`: {kv.Value}"));

                sb.AppendLine($"| {p.BucketUtc:yyyy-MM-dd} | {p.ChangeCount} | {topTypes} |");
            }

            return sb.ToString();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(ex, "Exec digest: compliance drift section omitted for tenant {TenantId}.", tenantId);

            return null;
        }
    }

    private async Task<(int? manifestCount, List<ExecDigestHighlightedRun> highlights, string? latestRunHex, string? findingsDelta)>
        TryBuildManifestAndFindingSectionsAsync(
            ScopeContext authorityScope,
            DateTime weekStartUtcInclusive,
            DateTime weekEndUtcExclusive,
            CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<RunSummaryDto> summaries =
                await _authorityQueryService.ListRunsByProjectAsync(authorityScope, "default", MaxListRuns, cancellationToken);

            List<Guid> candidateRunIds = summaries
                .Where(static s => s.HasGoldenManifest)
                .Select(static s => s.RunId)
                .Distinct()
                .Take(MaxRunDetailLookups)
                .ToList();

            List<(Guid RunId, DateTime? CommittedUtc, int Score)> scored = [];


            foreach (Guid runId in candidateRunIds)
            {
                if (scored.Count >= MaxRunDetailLookups)
                    break;


                string runHex = runId.ToString("N");
                ArchitectureRunDetail? detail =
                    await _runDetailQueryService.GetRunDetailAsync(runHex, cancellationToken);

                if (detail is null)
                    continue;


                if (detail.Run.Status is not ArchitectureRunStatus.Committed)
                    continue;


                DateTime? committedUtc = detail.Manifest?.Metadata?.CreatedUtc;

                if (committedUtc is null)
                    continue;


                if (committedUtc < weekStartUtcInclusive || committedUtc >= weekEndUtcExclusive)
                    continue;


                PilotRunDeltas deltas = await _pilotRunDeltaComputer.ComputeAsync(detail, cancellationToken);
                int score = deltas.FindingsBySeverity.Sum(static p => p.Value);
                scored.Add((runId, committedUtc, score));
            }

            int manifestCount = scored.Count;

            List<ExecDigestHighlightedRun> highlights = scored
                .OrderByDescending(static x => x.Score)
                .ThenByDescending(static x => x.CommittedUtc)
                .Take(TopRunCount)
                .Select(
                    static x => new ExecDigestHighlightedRun(
                        x.RunId.ToString("N"),
                        x.Score,
                        x.CommittedUtc is { } c ? $"Committed {c:yyyy-MM-dd} UTC" : null))
                .ToList();

            string? latestHex = scored
                .OrderByDescending(static x => x.CommittedUtc)
                .Select(static x => x.RunId.ToString("N"))
                .FirstOrDefault();

            string? findingsDelta = await TryBuildFindingsDeltaAsync(scored, cancellationToken);

            return (manifestCount == 0 ? null : manifestCount, highlights, latestHex, findingsDelta);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(ex, "Exec digest: manifest/findings sections omitted.");

            return (null, [], null, null);
        }
    }

    private async Task<string?> TryBuildFindingsDeltaAsync(
        IReadOnlyList<(Guid RunId, DateTime? CommittedUtc, int Score)> scored,
        CancellationToken cancellationToken)
    {
        if (scored.Count < 2)
            return null;


        List<(Guid RunId, DateTime? CommittedUtc, int Score)> ordered = scored
            .OrderBy(static x => x.CommittedUtc)
            .ToList();

        (Guid olderId, _, _) = ordered[0];
        (Guid newerId, _, _) = ordered[^1];

        try
        {
            ArchitectureRunDetail? older =
                await _runDetailQueryService.GetRunDetailAsync(olderId.ToString("N"), cancellationToken);

            ArchitectureRunDetail? newer =
                await _runDetailQueryService.GetRunDetailAsync(newerId.ToString("N"), cancellationToken);

            if (older is null || newer is null)
                return null;


            PilotRunDeltas olderDeltas = await _pilotRunDeltaComputer.ComputeAsync(older, cancellationToken);
            PilotRunDeltas newerDeltas = await _pilotRunDeltaComputer.ComputeAsync(newer, cancellationToken);

            int olderTotal = olderDeltas.FindingsBySeverity.Sum(static p => p.Value);
            int newerTotal = newerDeltas.FindingsBySeverity.Sum(static p => p.Value);

            return $"Findings (total severities) moved from {olderTotal} → {newerTotal} between earliest and latest commits in this UTC window.";
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(ex, "Exec digest: findings delta omitted.");

            return null;
        }
    }

    private static string NormalizeBaseUrl(string operatorBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(operatorBaseUrl))
            return "http://localhost:3000";

        return operatorBaseUrl.Trim().TrimEnd('/');
    }

    private static string FormatWeekLabel(DateTime startUtc, DateTime endUtc)
    {
        int isoYear = ISOWeek.GetYear(startUtc.Date);
        int isoWeek = ISOWeek.GetWeekOfYear(startUtc.Date);

        return $"{startUtc:yyyy-MM-dd}–{endUtc.AddTicks(-1):yyyy-MM-dd} UTC (ISO week {isoYear}-W{isoWeek:00})";
    }
}
