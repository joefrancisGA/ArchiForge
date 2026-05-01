using System.Globalization;
using System.Text.Json;

using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Llm.Redaction;
using ArchLucid.Persistence.Serialization;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Governance;

/// <inheritdoc cref="IPolicyPackDryRunService" />
/// <remarks>
///     <para>
///         <strong>Redaction is mandatory:</strong> every dry-run serialises the proposed thresholds to JSON
///         and runs the result through <see cref="IPromptRedactor.Redact" /> <em>before</em> the audit row is
///         built. Bypassing the redactor (e.g. a future direct call to <c>JsonSerializer.Serialize</c> in the
///         audit payload path) violates PENDING_QUESTIONS Q37; the integration test
///         <c>PolicyPackDryRunIntegrationTests</c> guards the marker.
///     </para>
///     <para>
///         <strong>Audit-on-failure semantics:</strong> the audit row is written via
///         <see cref="DurableAuditLogRetry.TryLogAsync" /> after the response is built. Audit I/O failures
///         are logged but do not break the read-only what-if response — same pattern as
///         <c>GovernanceWorkflowService</c>. The response body always reflects the redacted thresholds
///         that <em>would</em> have been persisted, so reviewers see the marker even when the audit row
///         later fails to land.
///     </para>
/// </remarks>
public sealed class PolicyPackDryRunService(
    IRunDetailQueryService runDetailQueryService,
    IPilotRunDeltaComputer pilotRunDeltaComputer,
    IPromptRedactor promptRedactor,
    IAuditService auditService,
    ILogger<PolicyPackDryRunService> logger) : IPolicyPackDryRunService
{
    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ILogger<PolicyPackDryRunService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPilotRunDeltaComputer _pilotRunDeltaComputer =
        pilotRunDeltaComputer ?? throw new ArgumentNullException(nameof(pilotRunDeltaComputer));

    private readonly IPromptRedactor _promptRedactor =
        promptRedactor ?? throw new ArgumentNullException(nameof(promptRedactor));

    private readonly IRunDetailQueryService _runDetailQueryService =
        runDetailQueryService ?? throw new ArgumentNullException(nameof(runDetailQueryService));

    /// <inheritdoc />
    public async Task<PolicyPackDryRunResponse> EvaluateAsync(
        Guid policyPackId,
        IReadOnlyDictionary<string, string> proposedThresholds,
        IReadOnlyList<string> evaluateAgainstRunIds,
        int? pageSize,
        int? page,
        CancellationToken cancellationToken = default)
    {
        if (proposedThresholds is null)
            throw new ArgumentNullException(nameof(proposedThresholds));
        if (evaluateAgainstRunIds is null)
            throw new ArgumentNullException(nameof(evaluateAgainstRunIds));

        int clampedPageSize = ClampPageSize(pageSize);
        List<string> cleanedRunIds = evaluateAgainstRunIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Take(IPolicyPackDryRunService.MaxEvaluatedRuns)
            .ToList();

        Dictionary<string, double> parsedThresholds = ParseThresholds(proposedThresholds);

        string redactedThresholdsJson = RedactProposedThresholdsJson(proposedThresholds);

        List<PolicyPackDryRunRunItem> allItems = [];

        foreach (string runId in cleanedRunIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PolicyPackDryRunRunItem item = await EvaluateSingleRunAsync(runId, parsedThresholds, cancellationToken);
            allItems.Add(item);
        }

        PolicyPackDryRunDeltaCounts deltaCounts = TallyDeltaCounts(allItems);

        int clampedPage = ClampPage(page, allItems.Count, clampedPageSize);
        int skip = (clampedPage - 1) * clampedPageSize;
        List<PolicyPackDryRunRunItem> pageItems = allItems
            .Skip(skip)
            .Take(clampedPageSize)
            .ToList();

        PolicyPackDryRunResponse response = new()
        {
            PolicyPackId = policyPackId,
            EvaluatedUtc = DateTime.UtcNow,
            Page = clampedPage,
            PageSize = clampedPageSize,
            TotalRequestedRuns = cleanedRunIds.Count,
            ReturnedRuns = pageItems.Count,
            ProposedThresholdsRedactedJson = redactedThresholdsJson,
            DeltaCounts = deltaCounts,
            Items = pageItems
        };

        await TryLogAuditAsync(policyPackId, redactedThresholdsJson, cleanedRunIds, deltaCounts, cancellationToken);

        return response;
    }

    private static int ClampPageSize(int? pageSize)
    {
        return pageSize is null
            ? IPolicyPackDryRunService.DefaultPageSize
            : Math.Clamp(pageSize.Value, 1, IPolicyPackDryRunService.MaxPageSize);
    }

    private static int ClampPage(int? page, int totalItems, int pageSize)
    {
        if (totalItems == 0)
            return 1;

        int requested = page.GetValueOrDefault(1);
        int maxPage = (int)Math.Ceiling(totalItems / (double)pageSize);

        return Math.Clamp(requested, 1, Math.Max(1, maxPage));
    }

    private async Task<PolicyPackDryRunRunItem> EvaluateSingleRunAsync(
        string runId,
        IReadOnlyDictionary<string, double> parsedThresholds,
        CancellationToken cancellationToken)
    {
        ArchitectureRunDetail? detail = await TryLoadRunDetailAsync(runId, cancellationToken);

        if (detail is null)
            return new PolicyPackDryRunRunItem { RunId = runId, RunMissing = true };

        PilotRunDeltas deltas = await _pilotRunDeltaComputer.ComputeAsync(detail, cancellationToken);

        IReadOnlyList<PolicyPackDryRunSeverityCount> findingsBySeverity = ProjectSeverityCounts(deltas);
        IReadOnlyList<PolicyPackDryRunThresholdOutcome> outcomes =
            ComputeThresholdOutcomes(parsedThresholds, deltas);

        bool wouldBlock = outcomes.Any(o => o.WouldBreach);

        return new PolicyPackDryRunRunItem
        {
            RunId = runId,
            RunMissing = false,
            FindingsBySeverity = findingsBySeverity,
            ThresholdOutcomes = outcomes,
            WouldBlock = wouldBlock
        };
    }

    private async Task<ArchitectureRunDetail?> TryLoadRunDetailAsync(string runId, CancellationToken cancellationToken)
    {
        try
        {
            return await _runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Dry-run: failed to load run {RunId}; treating as missing.", runId);
            return null;
        }
    }

    private static IReadOnlyList<PolicyPackDryRunSeverityCount> ProjectSeverityCounts(PilotRunDeltas deltas)
    {
        return deltas.FindingsBySeverity
            .Select(p => new PolicyPackDryRunSeverityCount { Severity = p.Key, Count = p.Value })
            .ToList();
    }

    /// <summary>
    ///     Maps each supported threshold key to a per-run breach result. Keys not present in
    ///     <paramref name="parsedThresholds" /> are skipped — the caller is opting out of evaluating that
    ///     metric, not setting it to "infinity".
    /// </summary>
    internal static IReadOnlyList<PolicyPackDryRunThresholdOutcome> ComputeThresholdOutcomes(
        IReadOnlyDictionary<string, double> parsedThresholds,
        PilotRunDeltas deltas)
    {
        List<PolicyPackDryRunThresholdOutcome> outcomes = [];

        foreach (string key in PolicyPackDryRunSupportedThresholdKeys.All)
        {
            if (!parsedThresholds.TryGetValue(key, out double proposed))
                continue;

            double actual = ComputeActualForKey(key, deltas);
            bool breach = actual > proposed;

            outcomes.Add(new PolicyPackDryRunThresholdOutcome
            {
                Key = key, ProposedValue = proposed, ActualValue = actual, WouldBreach = breach
            });
        }

        return outcomes;
    }

    private static double ComputeActualForKey(string key, PilotRunDeltas deltas)
    {
        return key switch
        {
            PolicyPackDryRunSupportedThresholdKeys.MaxCriticalFindings => CountForSeverity(deltas, "critical"),
            PolicyPackDryRunSupportedThresholdKeys.MaxHighFindings => CountForSeverity(deltas, "error"),
            PolicyPackDryRunSupportedThresholdKeys.MaxTotalFindings => deltas.FindingsBySeverity.Sum(p => p.Value),
            PolicyPackDryRunSupportedThresholdKeys.MaxTimeToCommitMinutes => deltas.TimeToCommittedManifest
                ?.TotalMinutes ?? 0d,
            _ => 0d
        };
    }

    private static double CountForSeverity(PilotRunDeltas deltas, string severity)
    {
        return deltas.FindingsBySeverity
            .Where(p => string.Equals(p.Key, severity, StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Value);
    }

    private static PolicyPackDryRunDeltaCounts TallyDeltaCounts(IReadOnlyList<PolicyPackDryRunRunItem> items)
    {
        return new PolicyPackDryRunDeltaCounts
        {
            Evaluated = items.Count,
            WouldBlock = items.Count(i => i is { RunMissing: false, WouldBlock: true }),
            WouldAllow = items.Count(i => i is { RunMissing: false, WouldBlock: false }),
            RunMissing = items.Count(i => i.RunMissing)
        };
    }

    /// <summary>
    ///     Parses opaque <c>string</c> threshold values to <see cref="double" />. Invalid / non-numeric
    ///     values are silently skipped — the caller experimented with a malformed input, not the system's
    ///     fault to error on; the threshold simply isn't evaluated. Skips also keep the redaction
    ///     pipeline safely between request body and audit row even if the value contained PII.
    /// </summary>
    internal static Dictionary<string, double> ParseThresholds(IReadOnlyDictionary<string, string> proposedThresholds)
    {
        Dictionary<string, double> parsed = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> entry in proposedThresholds)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
                continue;
            if (string.IsNullOrWhiteSpace(entry.Value))
                continue;

            if (!double.TryParse(entry.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                continue;

            parsed[entry.Key.Trim()] = value;
        }

        return parsed;
    }

    /// <summary>
    ///     Serialises <paramref name="proposedThresholds" /> to JSON, then routes the result through
    ///     <see cref="IPromptRedactor" /> so any PII pasted into a threshold value is replaced with the
    ///     configured replacement token (default <c>[REDACTED]</c>) before it lands in
    ///     <c>dbo.AuditEvents</c>. This is the single redaction seam mandated by PENDING_QUESTIONS Q37.
    /// </summary>
    internal string RedactProposedThresholdsJson(IReadOnlyDictionary<string, string> proposedThresholds)
    {
        string raw = JsonSerializer.Serialize(proposedThresholds, AuditJsonSerializationOptions.Instance);
        PromptRedactionOutcome outcome = _promptRedactor.Redact(raw);

        return outcome.Text;
    }

    private async Task TryLogAuditAsync(
        Guid policyPackId,
        string proposedThresholdsRedactedJson,
        IReadOnlyList<string> evaluatedRunIds,
        PolicyPackDryRunDeltaCounts deltaCounts,
        CancellationToken cancellationToken)
    {
        string dataJson = JsonSerializer.Serialize(
            new
            {
                policyPackId,
                proposedThresholdsRedacted = proposedThresholdsRedactedJson,
                evaluatedRunIds,
                deltaCounts = new
                {
                    evaluated = deltaCounts.Evaluated,
                    wouldBlock = deltaCounts.WouldBlock,
                    wouldAllow = deltaCounts.WouldAllow,
                    runMissing = deltaCounts.RunMissing
                }
            },
            AuditJsonSerializationOptions.Instance);

        AuditEvent auditEvent = new() { EventType = AuditEventTypes.GovernanceDryRunRequested, DataJson = dataJson };

        await DurableAuditLogRetry.TryLogAsync(
            ct => _auditService.LogAsync(auditEvent, ct),
            _logger,
            $"GovernanceDryRunRequested:{policyPackId:D}",
            cancellationToken);
    }
}
