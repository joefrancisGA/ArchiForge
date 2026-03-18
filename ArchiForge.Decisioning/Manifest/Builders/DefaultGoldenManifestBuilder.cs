using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Manifest.Builders;

public class DefaultGoldenManifestBuilder : IGoldenManifestBuilder
{
    public GoldenManifest Build(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        FindingsSnapshot findingsSnapshot,
        DecisionTrace trace,
        DecisionRuleSet ruleSet)
    {
        var manifest = new GoldenManifest
        {
            ManifestId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = contextSnapshotId,
            GraphSnapshotId = graphSnapshot.GraphSnapshotId,
            FindingsSnapshotId = findingsSnapshot.FindingsSnapshotId,
            DecisionTraceId = trace.DecisionTraceId,
            CreatedUtc = DateTime.UtcNow,
            RuleSetId = ruleSet.RuleSetId,
            RuleSetVersion = ruleSet.Version,
            RuleSetHash = ruleSet.RuleSetHash,
            Metadata = new ManifestMetadata
            {
                Name = $"ArchiForge Manifest {runId:N}",
                Version = "1.0.0",
                Status = "Draft",
                Summary = "Resolved architecture state generated from graph findings and rule evaluation."
            }
        };

        PopulateRequirements(manifest, findingsSnapshot);
        PopulateTopology(manifest, findingsSnapshot);
        PopulateSecurity(manifest, findingsSnapshot);
        PopulateCost(manifest, findingsSnapshot);
        PopulateConstraints(manifest, findingsSnapshot, trace);
        PopulateProvenance(manifest, findingsSnapshot, trace);

        manifest.Metadata.Status = manifest.UnresolvedIssues.Items.Count == 0
            ? "Resolved"
            : "NeedsAttention";

        return manifest;
    }

    private static void PopulateRequirements(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (var finding in findingsSnapshot.GetByType("RequirementFinding"))
        {
            var payload = FindingPayloadConverter.ToRequirementPayload(finding);
            if (payload is null)
                continue;

            var item = new RequirementCoverageItem
            {
                RequirementName = payload.RequirementName,
                RequirementText = payload.RequirementText,
                IsMandatory = payload.IsMandatory,
                CoverageStatus = "Covered",
                SupportingFindingIds = new List<string> { finding.FindingId }
            };

            manifest.Requirements.Covered.Add(item);

            manifest.Decisions.Add(new ResolvedArchitectureDecision
            {
                Category = "Requirement",
                Title = payload.RequirementName,
                SelectedOption = "Accepted",
                Rationale = payload.RequirementText,
                SupportingFindingIds = new List<string> { finding.FindingId }
            });
        }
    }

    private static void PopulateTopology(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (var finding in findingsSnapshot.GetByType("TopologyGap"))
        {
            var payload = FindingPayloadConverter.ToTopologyGapPayload(finding);

            var description = payload?.Description ?? finding.Title;
            manifest.Topology.Gaps.Add(description);
            manifest.Warnings.Add(description);

            manifest.UnresolvedIssues.Items.Add(new ManifestIssue
            {
                IssueType = "TopologyGap",
                Title = finding.Title,
                Description = payload?.Impact ?? finding.Rationale,
                Severity = finding.Severity.ToString(),
                SupportingFindingIds = new List<string> { finding.FindingId }
            });
        }
    }

    private static void PopulateSecurity(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (var finding in findingsSnapshot.GetByType("SecurityControlFinding"))
        {
            var payload = FindingPayloadConverter.ToSecurityControlPayload(finding);
            if (payload is null)
                continue;

            manifest.Security.Controls.Add(new SecurityPostureItem
            {
                ControlId = payload.ControlId,
                ControlName = payload.ControlName,
                Status = payload.Status,
                Impact = payload.Impact
            });

            if (string.Equals(payload.Status, "missing", StringComparison.OrdinalIgnoreCase))
            {
                manifest.Security.Gaps.Add($"{payload.ControlName} is missing");
                manifest.UnresolvedIssues.Items.Add(new ManifestIssue
                {
                    IssueType = "SecurityGap",
                    Title = $"Missing security control: {payload.ControlName}",
                    Description = payload.Impact,
                    Severity = finding.Severity.ToString(),
                    SupportingFindingIds = new List<string> { finding.FindingId }
                });

                manifest.Decisions.Add(new ResolvedArchitectureDecision
                {
                    Category = "Security",
                    Title = $"Enforce control: {payload.ControlName}",
                    SelectedOption = "RequiredRemediation",
                    Rationale = payload.Impact,
                    SupportingFindingIds = new List<string> { finding.FindingId }
                });
            }
        }
    }

    private static void PopulateCost(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (var finding in findingsSnapshot.GetByType("CostConstraintFinding"))
        {
            var payload = FindingPayloadConverter.ToCostConstraintPayload(finding);
            if (payload is null)
                continue;

            if (payload.MaxMonthlyCost.HasValue)
                manifest.Cost.MaxMonthlyCost = payload.MaxMonthlyCost.Value;

            if (!string.IsNullOrWhiteSpace(payload.CostRisk))
                manifest.Cost.CostRisks.Add(payload.CostRisk);
        }
    }

    private static void PopulateConstraints(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        DecisionTrace trace)
    {
        foreach (var findingId in trace.AcceptedFindingIds)
        {
            var finding = findingsSnapshot.Findings.FirstOrDefault(f => f.FindingId == findingId);
            if (finding is null)
                continue;

            if (finding.Severity == FindingSeverity.Critical || finding.Severity == FindingSeverity.Error)
            {
                manifest.Constraints.MandatoryConstraints.Add(finding.Title);
            }
            else if (finding.Severity == FindingSeverity.Info || finding.Severity == FindingSeverity.Warning)
            {
                manifest.Constraints.Preferences.Add(finding.Title);
            }
        }
    }

    private static void PopulateProvenance(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        DecisionTrace trace)
    {
        manifest.Provenance.SourceFindingIds = findingsSnapshot.Findings
            .Select(f => f.FindingId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Provenance.SourceGraphNodeIds = findingsSnapshot.Findings
            .SelectMany(f => f.RelatedNodeIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Provenance.AppliedRuleIds = trace.AppliedRuleIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

