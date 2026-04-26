using System.Globalization;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Findings.Factories;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph;
using ArchLucid.KnowledgeGraph.Models;

using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Manifest.Builders;

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
        RuleAuditTracePayload audit = trace.RequireRuleAudit();

        GoldenManifest manifest = new()
        {
            ManifestId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = contextSnapshotId,
            GraphSnapshotId = graphSnapshot.GraphSnapshotId,
            FindingsSnapshotId = findingsSnapshot.FindingsSnapshotId,
            DecisionTraceId = audit.DecisionTraceId,
            CreatedUtc = DateTime.UtcNow,
            RuleSetId = ruleSet.RuleSetId,
            RuleSetVersion = ruleSet.Version,
            RuleSetHash = ruleSet.RuleSetHash,
            Metadata = new ManifestMetadata
            {
                Name = $"ArchLucid Manifest {runId:N}",
                Version = "1.0.0",
                Status = "Draft",
                Summary = "Resolved architecture state generated from graph findings and rule evaluation."
            }
        };

        PopulateRequirements(manifest, findingsSnapshot);
        PopulateTopologyFromGraph(manifest, graphSnapshot);
        PopulateTypedTopologyFromGraph(manifest, graphSnapshot);
        PopulateTopology(manifest, findingsSnapshot);
        PopulateSecurity(manifest, findingsSnapshot);
        PopulateCompliance(manifest, findingsSnapshot);
        PopulateCost(manifest, findingsSnapshot);
        PopulatePolicyApplicability(manifest, findingsSnapshot);
        PopulatePolicySection(manifest, findingsSnapshot);
        PopulateCoverageWarnings(manifest, findingsSnapshot);
        PopulateConstraints(manifest, findingsSnapshot, audit);
        PopulateProvenance(manifest, findingsSnapshot, audit);

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
        manifest.Topology.Services = manifest.Topology.Services
            .OrderBy(x => x.ServiceName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Topology.Datastores = manifest.Topology.Datastores
            .OrderBy(x => x.DatastoreName, StringComparer.OrdinalIgnoreCase)
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

        manifest.Compliance.Controls = manifest.Compliance.Controls
            .OrderBy(x => x.ControlName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Compliance.Gaps = manifest.Compliance.Gaps
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

        manifest.Policy.SatisfiedControls = manifest.Policy.SatisfiedControls
            .OrderBy(x => x.ControlName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Policy.Violations = manifest.Policy.Violations
            .OrderBy(x => x.ControlName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Policy.Exemptions = manifest.Policy.Exemptions
            .OrderBy(x => x.ControlId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Policy.Notes = manifest.Policy.Notes
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void PopulateRequirements(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.RequirementFinding))
        {
            RequirementFindingPayload? payload = FindingPayloadConverter.ToRequirementPayload(finding);
            if (payload is null)
                continue;

            RequirementCoverageItem item = new()
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
        foreach (GraphNode node in graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource))
        {
            if (string.IsNullOrWhiteSpace(node.Label))
                continue;
            manifest.Topology.Resources.Add(node.Label);
        }
    }

    /// <summary>
    ///     PR A0.5 / owner 35f — <see cref="GraphNode.Properties" /> carry optional
    ///     <c>serviceType</c>, <c>runtimePlatform</c>, <c>datastoreType</c> keys (enum names, case-insensitive).
    /// </summary>
    private static void PopulateTypedTopologyFromGraph(GoldenManifest manifest, GraphSnapshot graphSnapshot)
    {
        foreach (GraphNode node in graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource))
        {
            if (string.IsNullOrWhiteSpace(node.Label))
                continue;

            string? category = node.Category;
            bool isDatastore = string.Equals(category, GraphTopologyCategories.Data, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(category, GraphTopologyCategories.Storage,
                                   StringComparison.OrdinalIgnoreCase);

            if (isDatastore)
            {
                manifest.Topology.Datastores.Add(
                    new Cm.ManifestDatastore
                    {
                        DatastoreId = node.NodeId,
                        DatastoreName = node.Label,
                        DatastoreType = ParseEnumKey<DatastoreType>(node.Properties, "datastoreType"),
                        RuntimePlatform = ParseEnumKey<RuntimePlatform>(node.Properties, "runtimePlatform")
                    });
            }
            else
            {
                manifest.Topology.Services.Add(
                    new Cm.ManifestService
                    {
                        ServiceId = node.NodeId,
                        ServiceName = node.Label,
                        ServiceType = ParseEnumKey<ServiceType>(node.Properties, "serviceType"),
                        RuntimePlatform = ParseEnumKey<RuntimePlatform>(node.Properties, "runtimePlatform")
                    });
            }
        }
    }

    private static TEnum ParseEnumKey<TEnum>(Dictionary<string, string> properties, string key)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(key))
            return default;

        string? raw = null;

        foreach (KeyValuePair<string, string> kv in properties)
        {
            if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                continue;

            raw = kv.Value;
            break;
        }

        if (string.IsNullOrWhiteSpace(raw))
            return default;

        return Enum.TryParse(raw, true, out TEnum e) ? e : default;
    }

    private static void PopulateTopology(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.TopologyGap))
        {
            TopologyGapFindingPayload? payload = FindingPayloadConverter.ToTopologyGapPayload(finding);

            string description = payload?.Description ?? finding.Title;
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
        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.SecurityControlFinding))
        {
            SecurityControlFindingPayload? payload = FindingPayloadConverter.ToSecurityControlPayload(finding);

            if (payload is null)
                continue;

            manifest.Security.Controls.Add(new SecurityPostureItem
            {
                ControlId = payload.ControlId,
                ControlName = payload.ControlName,
                Status = payload.Status,
                Impact = payload.Impact
            });

            if (!string.Equals(payload.Status, "missing", StringComparison.OrdinalIgnoreCase))
                continue;

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

    private static void PopulateCompliance(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot)
    {
        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.ComplianceFinding))
        {
            ComplianceFindingPayload? payload = FindingPayloadConverter.ToCompliancePayload(finding);
            if (payload is null)
                continue;

            manifest.Compliance.Controls.Add(new CompliancePostureItem
            {
                ControlId = payload.ControlId,
                ControlName = payload.ControlName,
                AppliesToCategory = payload.AppliesToCategory,
                Status = "Gap"
            });

            if (payload.AffectedResources.Count > 0)

                manifest.Compliance.Gaps.Add(
                    $"{payload.ControlName}: {string.Join(", ", payload.AffectedResources)}");


            manifest.UnresolvedIssues.Items.Add(new ManifestIssue
            {
                IssueType = "ComplianceGap",
                Title = finding.Title,
                Description = finding.Rationale,
                Severity = finding.Severity.ToString(),
                SupportingFindingIds = [finding.FindingId]
            });
        }
    }

    private static void PopulateCost(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.CostConstraintFinding))
        {
            CostConstraintFindingPayload? payload = FindingPayloadConverter.ToCostConstraintPayload(finding);
            if (payload is null)
                continue;

            if (payload.MaxMonthlyCost.HasValue)
                manifest.Cost.MaxMonthlyCost = payload.MaxMonthlyCost.Value;

            if (!string.IsNullOrWhiteSpace(payload.CostRisk))
                manifest.Cost.CostRisks.Add(payload.CostRisk);

            string budgetLabel = string.IsNullOrWhiteSpace(payload.BudgetName) ? "default" : payload.BudgetName;
            string capText = payload.MaxMonthlyCost.HasValue
                ? payload.MaxMonthlyCost.Value.ToString("N0", CultureInfo.InvariantCulture)
                : "unspecified";

            manifest.Assumptions.Add(
                $"Preferred: Cost targets align with budget '{budgetLabel}' (monthly cap {capText}, risk {payload.CostRisk}).");
        }
    }

    private static void PopulatePolicyApplicability(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.PolicyApplicabilityFinding))
        {
            PolicyApplicabilityFindingPayload? payload = FindingPayloadConverter.ToPolicyApplicabilityPayload(finding);
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

                manifest.Assumptions.Add(
                    $"Policy '{payload.PolicyName}' applies to {payload.ApplicableTopologyResourceCount} topology resource(s) (APPLIES_TO in knowledge graph).");
        }
    }

    private static void PopulatePolicySection(GoldenManifest manifest, FindingsSnapshot findingsSnapshot)
    {
        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.PolicyApplicabilityFinding))
        {
            PolicyApplicabilityFindingPayload? payload = FindingPayloadConverter.ToPolicyApplicabilityPayload(finding);
            if (payload is null)
                continue;

            string pack = string.IsNullOrWhiteSpace(payload.PolicyReference) ? "Inferred" : payload.PolicyReference!;
            string controlId = string.IsNullOrWhiteSpace(payload.PolicyReference)
                ? payload.PolicyName
                : payload.PolicyReference!;

            if (finding.Severity == FindingSeverity.Info)

                manifest.Policy.SatisfiedControls.Add(new PolicyControlItem
                {
                    ControlId = controlId,
                    ControlName = payload.PolicyName,
                    PolicyPack = pack,
                    Description =
                        $"{payload.ApplicableTopologyResourceCount} topology resource(s) in APPLIES_TO scope."
                });

            else if (finding.Severity == FindingSeverity.Warning)

                manifest.Policy.Violations.Add(new PolicyControlItem
                {
                    ControlId = controlId,
                    ControlName = payload.PolicyName,
                    PolicyPack = pack,
                    Description = string.IsNullOrWhiteSpace(finding.Rationale) ? finding.Title : finding.Rationale
                });
        }

        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.PolicyCoverageFinding))
        {
            PolicyCoverageFindingPayload? payload = FindingPayloadConverter.ToPolicyCoveragePayload(finding);
            if (payload is null)
                continue;

            if (payload.UncoveredResources.Count == 0)
            {
                manifest.Policy.Violations.Add(new PolicyControlItem
                {
                    ControlId = "policy-coverage",
                    ControlName = "Policy topology coverage",
                    PolicyPack = "Governance",
                    Description = string.IsNullOrWhiteSpace(finding.Rationale) ? finding.Title : finding.Rationale
                });

                continue;
            }

            foreach (string resource in payload.UncoveredResources)

                manifest.Policy.Violations.Add(new PolicyControlItem
                {
                    ControlId = "policy-coverage",
                    ControlName = $"Uncovered: {resource}",
                    PolicyPack = "Governance",
                    Description = finding.Title
                });
        }
    }

    private static void PopulateCoverageWarnings(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot)
    {
        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.TopologyCoverageFinding))
        {
            TopologyCoverageFindingPayload? payload = FindingPayloadConverter.ToTopologyCoveragePayload(finding);
            if (payload is null || payload.MissingCategories.Count == 0)
                continue;

            foreach (string category in payload.MissingCategories)
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

        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.SecurityCoverageFinding))
        {
            SecurityCoverageFindingPayload? payload = FindingPayloadConverter.ToSecurityCoveragePayload(finding);
            if (payload is null)
                continue;

            foreach (string resource in payload.UnprotectedResources)
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

        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.PolicyCoverageFinding))
        {
            PolicyCoverageFindingPayload? payload = FindingPayloadConverter.ToPolicyCoveragePayload(finding);
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

        foreach (Finding finding in findingsSnapshot.GetByType(FindingTypes.RequirementCoverageFinding))
        {
            RequirementCoverageFindingPayload? payload = FindingPayloadConverter.ToRequirementCoveragePayload(finding);
            if (payload is null)
                continue;

            foreach (string req in payload.UncoveredRequirements)

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

    private static void PopulateConstraints(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        RuleAuditTracePayload trace)
    {
        foreach (Finding finding in trace.AcceptedFindingIds
                     .Select(findingId => findingsSnapshot.Findings.FirstOrDefault(f => f.FindingId == findingId))
                     .OfType<Finding>())

            if (finding.Severity is FindingSeverity.Critical or FindingSeverity.Error)

                manifest.Constraints.MandatoryConstraints.Add(finding.Title);

            else if (finding.Severity is FindingSeverity.Info or FindingSeverity.Warning)

                manifest.Constraints.Preferences.Add(finding.Title);
    }

    private static void PopulateProvenance(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        RuleAuditTracePayload trace)
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
