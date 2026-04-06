using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

/// <summary>
/// <see cref="IDecisionEngine"/> implementation that applies an ordered, priority-sorted
/// rule set to each finding in a <see cref="FindingsSnapshot"/>, then delegates manifest
/// construction to <see cref="IGoldenManifestBuilder"/>.
/// </summary>
/// <remarks>
/// Rules are applied in descending <c>Priority</c> order. For each finding the first matching
/// rule per action type wins; unmatched findings are recorded in
/// <see cref="RuleAuditTracePayload.Notes"/>. After manifest construction,
/// <see cref="IGoldenManifestValidator.Validate"/> is called and a content hash is computed
/// via <see cref="IManifestHashService"/>.
/// Cancellation is forwarded to <see cref="IDecisionRuleProvider.GetRuleSetAsync"/>; the
/// synchronous rule evaluation and manifest build steps do not observe the token.
/// </remarks>
public class RuleBasedDecisionEngine(
    IDecisionRuleProvider ruleProvider,
    IGoldenManifestBuilder manifestBuilder,
    IGoldenManifestValidator manifestValidator,
    IManifestHashService manifestHashService)
    : IDecisionEngine
{
    /// <inheritdoc />
    public async Task<(GoldenManifest Manifest, DecisionTrace Trace)> DecideAsync(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        FindingsSnapshot findingsSnapshot,
        CancellationToken ct)
    {
        DecisionRuleSet ruleSet = await ruleProvider.GetRuleSetAsync(ct);
        List<DecisionRule> rules = ruleSet.Rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        RuleAuditTracePayload audit = new()
        {
            DecisionTraceId = Guid.NewGuid(),
            RunId = runId,
            CreatedUtc = DateTime.UtcNow,
            RuleSetId = ruleSet.RuleSetId,
            RuleSetVersion = ruleSet.Version,
            RuleSetHash = ruleSet.RuleSetHash
        };

        foreach (Finding finding in findingsSnapshot.Findings)
        {
            List<DecisionRule> matchingRules = rules
                .Where(r => string.Equals(
                    r.AppliesToFindingType,
                    finding.FindingType,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingRules.Count == 0)
            {
                audit.Notes.Add($"No rule matched finding {finding.FindingId} ({finding.FindingType}).");
                continue;
            }

            foreach (DecisionRule rule in matchingRules)
            {
                audit.AppliedRuleIds.Add(rule.RuleId);

                switch (rule.Action.ToLowerInvariant())
                {
                    case "require":
                    case "allow":
                    case "prefer":
                        audit.AcceptedFindingIds.Add(finding.FindingId);
                        break;

                    case "reject":
                        audit.RejectedFindingIds.Add(finding.FindingId);
                        audit.Notes.Add($"Rejected finding {finding.FindingId} by rule {rule.Name}.");
                        break;

                    default:
                        audit.Notes.Add($"No recognized action for rule {rule.Name}.");
                        break;
                }
            }
        }

        DecisionTrace trace = DecisionTrace.FromRuleAudit(audit);

        GoldenManifest manifest = manifestBuilder.Build(
            runId,
            contextSnapshotId,
            graphSnapshot,
            findingsSnapshot,
            trace,
            ruleSet);

        manifestValidator.Validate(manifest);
        manifest.ManifestHash = manifestHashService.ComputeHash(manifest);

        return (manifest, trace);
    }
}

