using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

public class RuleBasedDecisionEngine(
    IDecisionRuleProvider ruleProvider,
    IGoldenManifestBuilder manifestBuilder,
    IGoldenManifestValidator manifestValidator,
    IGoldenManifestRepository manifestRepository,
    IDecisionTraceRepository traceRepository)
    : IDecisionEngine
{
    public async Task<(GoldenManifest Manifest, DecisionTrace Trace)> DecideAsync(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        FindingsSnapshot findingsSnapshot,
        CancellationToken ct)
    {
        var ruleSet = await ruleProvider.GetRuleSetAsync(ct);
        var rules = ruleSet.Rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        var trace = new DecisionTrace
        {
            DecisionTraceId = Guid.NewGuid(),
            RunId = runId,
            CreatedUtc = DateTime.UtcNow,
            RuleSetId = ruleSet.RuleSetId,
            RuleSetVersion = ruleSet.Version,
            RuleSetHash = ruleSet.RuleSetHash
        };

        foreach (var finding in findingsSnapshot.Findings)
        {
            var matchingRules = rules
                .Where(r => string.Equals(
                    r.AppliesToFindingType,
                    finding.FindingType,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingRules.Count == 0)
            {
                trace.Notes.Add($"No rule matched finding {finding.FindingId} ({finding.FindingType}).");
                continue;
            }

            foreach (var rule in matchingRules)
            {
                trace.AppliedRuleIds.Add(rule.RuleId);

                switch (rule.Action.ToLowerInvariant())
                {
                    case "require":
                    case "allow":
                    case "prefer":
                        trace.AcceptedFindingIds.Add(finding.FindingId);
                        break;

                    case "reject":
                        trace.RejectedFindingIds.Add(finding.FindingId);
                        trace.Notes.Add($"Rejected finding {finding.FindingId} by rule {rule.Name}.");
                        break;

                    default:
                        trace.Notes.Add($"No recognized action for rule {rule.Name}.");
                        break;
                }
            }
        }

        var manifest = manifestBuilder.Build(
            runId,
            contextSnapshotId,
            graphSnapshot,
            findingsSnapshot,
            trace,
            ruleSet);

        manifestValidator.Validate(manifest);
        manifest.ManifestHash = ComputeManifestHash(manifest);

        await traceRepository.SaveAsync(trace, ct);
        await manifestRepository.SaveAsync(manifest, ct);

        return (manifest, trace);
    }

    private static string ComputeManifestHash(GoldenManifest manifest)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            manifest.ManifestId,
            manifest.RunId,
            manifest.ContextSnapshotId,
            manifest.GraphSnapshotId,
            manifest.FindingsSnapshotId,
            manifest.DecisionTraceId,
            manifest.RuleSetId,
            manifest.RuleSetVersion,
            manifest.RuleSetHash,
            manifest.Metadata,
            manifest.Requirements,
            manifest.Topology,
            manifest.Security,
            manifest.Cost,
            manifest.Constraints,
            manifest.UnresolvedIssues,
            Decisions = manifest.Decisions
                .OrderBy(d => d.DecisionId)
                .Select(d => new
                {
                    d.DecisionId,
                    d.Category,
                    d.Title,
                    d.SelectedOption,
                    d.Rationale,
                    SupportingFindingIds = d.SupportingFindingIds.OrderBy(x => x).ToArray()
                })
                .ToArray(),
            Assumptions = manifest.Assumptions.OrderBy(x => x).ToArray(),
            Warnings = manifest.Warnings.OrderBy(x => x).ToArray(),
            manifest.Provenance
        });

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(canonical);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

