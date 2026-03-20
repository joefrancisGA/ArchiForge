using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Manifest.Sections;
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
        PopulateTopologyFromGraph(manifest, graphSnapshot);
        PopulateTopology(manifest, findingsSnapshot);
        PopulateSecurity(manifest, findingsSnapshot);
        PopulateCost(manifest, findingsSnapshot);
        PopulatePolicyApplicability(manifest, findingsSnapshot);
        PopulateCoverageWarnings(manifest, findingsSnapshot);
        PopulateConstraints(manifest, findingsSnapshot, trace);
        PopulateProvenance(manifest, findingsSnapshot, trace);

        manifest.Metadata.Status = manifest.UnresolvedIssues.Items.Count == 0
            ? "Resolved"
            : "NeedsAttention";

        NormalizeManifestOrdering(manifest);

        return manifest;
    }

    private static void NormalizeManifestOrdering(GoldenManifest manifest)
    {
        manifest.Requirements.Covered = manifest.Requirements.Covered
            .OrderBy(x => x.RequirementName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Requirements.Uncovered = manifest.Requirements.Uncovered
            .OrderBy(x => x.RequirementName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Topology.SelectedPatterns = manifest.Topology.SelectedPatterns
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Topology.Resources = manifest.Topology.Resources
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Topology.Gaps = manifest.Topology.Gaps
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Security.Controls = manifest.Security.Controls
            .OrderBy(x => x.ControlName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Security.Gaps = manifest.Security.Gaps
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Cost.CostRisks = manifest.Cost.CostRisks
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Cost.Notes = manifest.Cost.Notes
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.UnresolvedIssues.Items = manifest.UnresolvedIssues.Items
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Decisions = manifest.Decisions
            .OrderBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Assumptions = manifest.Assumptions
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Warnings = manifest.Warnings
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Constraints.MandatoryConstraints = manifest.Constraints.MandatoryConstraints
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Constraints.Preferences = manifest.Constraints.Preferences
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.Provenance.SourceFindingIds = manifest.Provenance.SourceFindingIds
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Provenance.SourceGraphNodeIds = manifest.Provenance.SourceGraphNodeIds
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Provenance.AppliedRuleIds = manifest.Provenance.AppliedRuleIds
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
                SupportingFindingIds = [finding.FindingId]
            };

            manifest.Requirements.Covered.Add(item);

            manifest.Decisions.Add(new ResolvedArchitectureDecision
            {
                Category = "Requirement",
                Title = payload.RequirementName,
                SelectedOption = "Accepted",
                Rationale = payload.RequirementText,
                SupportingFindingIds = [finding.FindingId]
            });
        }
    }

    private static void PopulateTopologyFromGraph(GoldenManifest manifest, GraphSnapshot graphSnapshot)
    {
        foreach (var node in graphSnapshot.GetNodesByType("TopologyResource"))
        {
            if (string.IsNullOrWhiteSpace(node.Label))
                continue;
            manifest.Topology.Resources.Add(node.Label);
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
                SupportingFindingIds = [finding.FindingId]
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
                    SupportingFindingIds = [finding.FindingId]
                });

                manifest.Decisions.Add(new ResolvedArchitectureDecision
                {
                    Category = "Security",
                    Title = $"Enforce control: {payload.ControlName}",
                    SelectedOption = "RequiredRemediation",
                    Rationale = payload.Impact,
                    SupportingFindingIds = [finding.FindingId]
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

    private static void PopulatePolicyApplicability(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (var finding in findingsSnapshot.GetByType("PolicyApplicabilityFinding"))
        {
            var payload = FindingPayloadConverter.ToPolicyApplicabilityPayload(finding);
            if (payload is null)
                continue;

            if (finding.Severity == FindingSeverity.Warning)
            {
                manifest.Warnings.Add($"{payload.PolicyName}: {finding.Title}");
                manifest.UnresolvedIssues.Items.Add(new ManifestIssue
                {
                    IssueType = "PolicyApplicabilityGap",
                    Title = finding.Title,
                    Description = finding.Rationale,
                    Severity = finding.Severity.ToString(),
                    SupportingFindingIds = [finding.FindingId]
                });
            }
            else if (finding.Severity == FindingSeverity.Info)
            {
                manifest.Assumptions.Add(
                    $"Policy '{payload.PolicyName}' applies to {payload.ApplicableTopologyResourceCount} topology resource(s) (APPLIES_TO in knowledge graph).");
            }
        }
    }

    private static void PopulateCoverageWarnings(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot)
    {
        foreach (var finding in findingsSnapshot.GetByType("TopologyCoverageFinding"))
        {
            var payload = FindingPayloadConverter.ToTopologyCoveragePayload(finding);
            if (payload is null || payload.MissingCategories.Count == 0)
                continue;

            foreach (var category in payload.MissingCategories)
                manifest.Topology.Gaps.Add($"Missing topology category: {category}");

            manifest.UnresolvedIssues.Items.Add(new ManifestIssue
            {
                IssueType = "TopologyCoverage",
                Title = finding.Title,
                Description = string.Join(", ", payload.MissingCategories),
                Severity = finding.Severity.ToString(),
                SupportingFindingIds = [finding.FindingId]
            });
        }

        foreach (var finding in findingsSnapshot.GetByType("SecurityCoverageFinding"))
        {
            var payload = FindingPayloadConverter.ToSecurityCoveragePayload(finding);
            if (payload is null)
                continue;

            foreach (var resource in payload.UnprotectedResources)
                manifest.Security.Gaps.Add($"{resource} is not protected");

            manifest.UnresolvedIssues.Items.Add(new ManifestIssue
            {
                IssueType = "SecurityCoverage",
                Title = finding.Title,
                Description = string.Join(", ", payload.UnprotectedResources),
                Severity = finding.Severity.ToString(),
                SupportingFindingIds = [finding.FindingId]
            });
        }

        foreach (var finding in findingsSnapshot.GetByType("PolicyCoverageFinding"))
        {
            var payload = FindingPayloadConverter.ToPolicyCoveragePayload(finding);
            if (payload is null)
                continue;

            manifest.UnresolvedIssues.Items.Add(new ManifestIssue
            {
                IssueType = "PolicyCoverage",
                Title = finding.Title,
                Description = payload.UncoveredResources.Count == 0
                    ? finding.Rationale
                    : string.Join(", ", payload.UncoveredResources),
                Severity = finding.Severity.ToString(),
                SupportingFindingIds = [finding.FindingId]
            });
        }

        foreach (var finding in findingsSnapshot.GetByType("RequirementCoverageFinding"))
        {
            var payload = FindingPayloadConverter.ToRequirementCoveragePayload(finding);
            if (payload is null)
                continue;

            foreach (var req in payload.UncoveredRequirements)
            {
                manifest.Requirements.Uncovered.Add(new RequirementCoverageItem
                {
                    RequirementName = req,
                    RequirementText = req,
                    IsMandatory = true,
                    CoverageStatus = "Uncovered",
                    SupportingFindingIds = [finding.FindingId]
                });
            }
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

