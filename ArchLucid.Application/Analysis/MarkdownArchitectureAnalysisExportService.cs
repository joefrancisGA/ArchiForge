using System.Text;

using ArchLucid.Application.Determinism;
using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Analysis;

public sealed class MarkdownArchitectureAnalysisExportService : IArchitectureAnalysisExportService
{
    public string GenerateMarkdown(ArchitectureAnalysisReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder sb = new();

        sb.AppendLine("# ArchLucid Analysis Report");
        sb.AppendLine();
        sb.AppendLine($"- Run ID: {report.Run.RunId}");
        sb.AppendLine($"- Request ID: {report.Run.RequestId}");
        sb.AppendLine($"- Run Status: {report.Run.Status}");
        sb.AppendLine($"- Created UTC: {report.Run.CreatedUtc:O}");

        if (report.Run.CompletedUtc.HasValue)

            sb.AppendLine($"- Completed UTC: {report.Run.CompletedUtc.Value:O}");

        if (!string.IsNullOrWhiteSpace(report.Run.CurrentManifestVersion))

            sb.AppendLine($"- Current Manifest Version: {report.Run.CurrentManifestVersion}");

        sb.AppendLine();

        if (report.Warnings.Count > 0)
        {
            sb.AppendLine("## Report Warnings");
            sb.AppendLine();

            foreach (string warning in report.Warnings)

                sb.AppendLine($"- {warning}");

            sb.AppendLine();
        }

        if (report.Evidence is not null)
        {
            sb.AppendLine("## Evidence Package");
            sb.AppendLine();
            sb.AppendLine($"- Evidence Package ID: {report.Evidence.EvidencePackageId}");
            sb.AppendLine($"- System Name: {report.Evidence.SystemName}");
            sb.AppendLine($"- Environment: {report.Evidence.Environment}");
            sb.AppendLine($"- Cloud Provider: {report.Evidence.CloudProvider}");
            sb.AppendLine();

            sb.AppendLine("### Request Context");
            sb.AppendLine();

            sb.AppendLine($"- Description: {report.Evidence.Request.Description}");

            if (report.Evidence.Request.Constraints.Count > 0)
            {
                sb.AppendLine("- Constraints:");
                foreach (string item in report.Evidence.Request.Constraints)

                    sb.AppendLine($"  - {item}");
            }

            if (report.Evidence.Request.RequiredCapabilities.Count > 0)
            {
                sb.AppendLine("- Required Capabilities:");
                foreach (string item in report.Evidence.Request.RequiredCapabilities)

                    sb.AppendLine($"  - {item}");
            }

            if (report.Evidence.Request.Assumptions.Count > 0)
            {
                sb.AppendLine("- Assumptions:");
                foreach (string item in report.Evidence.Request.Assumptions)

                    sb.AppendLine($"  - {item}");
            }

            sb.AppendLine();

            if (report.Evidence.Policies.Count > 0)
            {
                sb.AppendLine("### Policy Evidence");
                sb.AppendLine();

                foreach (PolicyEvidence policy in report.Evidence.Policies.OrderBy(x => x.Title))
                {
                    sb.AppendLine($"- **{policy.Title}**");
                    sb.AppendLine($"  - Policy ID: {policy.PolicyId}");
                    sb.AppendLine($"  - Summary: {policy.Summary}");

                    if (policy.RequiredControls.Count > 0)

                        sb.AppendLine($"  - Required Controls: {string.Join(", ", policy.RequiredControls)}");
                }

                sb.AppendLine();
            }

            if (report.Evidence.ServiceCatalog.Count > 0)
            {
                sb.AppendLine("### Service Catalog Hints");
                sb.AppendLine();

                foreach (ServiceCatalogEvidence service in report.Evidence.ServiceCatalog.OrderBy(x => x.ServiceName))
                {
                    sb.AppendLine($"- **{service.ServiceName}**");
                    sb.AppendLine($"  - Category: {service.Category}");
                    sb.AppendLine($"  - Summary: {service.Summary}");

                    if (service.RecommendedUseCases.Count > 0)

                        sb.AppendLine($"  - Recommended Use Cases: {string.Join(", ", service.RecommendedUseCases)}");
                }

                sb.AppendLine();
            }

            if (report.Evidence.Patterns.Count > 0)
            {
                sb.AppendLine("### Pattern Hints");
                sb.AppendLine();

                foreach (PatternEvidence pattern in report.Evidence.Patterns.OrderBy(x => x.Name))
                {
                    sb.AppendLine($"- **{pattern.Name}**");
                    sb.AppendLine($"  - Pattern ID: {pattern.PatternId}");
                    sb.AppendLine($"  - Summary: {pattern.Summary}");

                    if (pattern.SuggestedServices.Count > 0)

                        sb.AppendLine($"  - Suggested Services: {string.Join(", ", pattern.SuggestedServices)}");
                }

                sb.AppendLine();
            }
        }

        sb.AppendLine("## Agent Execution Traces");
        sb.AppendLine();

        if (report.ExecutionTraces.Count == 0)
        {
            sb.AppendLine("- No execution traces were found for this run.");
            sb.AppendLine();
        }
        else

            foreach (AgentExecutionTrace trace in report.ExecutionTraces
                         .OrderBy(x => x.AgentType)
                         .ThenBy(x => x.CreatedUtc))
            {
                sb.AppendLine($"### {trace.AgentType} — Task {trace.TaskId}");
                sb.AppendLine();
                sb.AppendLine($"- Trace ID: {trace.TraceId}");
                sb.AppendLine($"- Parse Succeeded: {(trace.ParseSucceeded ? "Yes" : "No")}");
                sb.AppendLine($"- Created UTC: {trace.CreatedUtc:O}");

                if (!string.IsNullOrWhiteSpace(trace.ErrorMessage))

                    sb.AppendLine($"- Error: {trace.ErrorMessage}");

                sb.AppendLine();
                sb.AppendLine("#### System Prompt");
                sb.AppendLine();
                sb.AppendLine("```text");
                sb.AppendLine(trace.SystemPrompt);
                sb.AppendLine("```");
                sb.AppendLine();

                sb.AppendLine("#### User Prompt");
                sb.AppendLine();
                sb.AppendLine("```text");
                sb.AppendLine(trace.UserPrompt);
                sb.AppendLine("```");
                sb.AppendLine();

                sb.AppendLine("#### Raw Response");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(trace.RawResponse);
                sb.AppendLine("```");
                sb.AppendLine();

                if (string.IsNullOrWhiteSpace(trace.ParsedResultJson))
                    continue;

                sb.AppendLine("#### Parsed Result");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(trace.ParsedResultJson);
                sb.AppendLine("```");
                sb.AppendLine();
            }

        if (report.Manifest is not null)
        {
            sb.AppendLine("## Architecture Manifest");
            sb.AppendLine();
            sb.AppendLine($"- System Name: {report.Manifest.SystemName}");
            sb.AppendLine($"- Run ID: {report.Manifest.RunId}");
            sb.AppendLine($"- Manifest Version: {report.Manifest.Metadata.ManifestVersion}");

            if (!string.IsNullOrWhiteSpace(report.Manifest.Metadata.ParentManifestVersion))

                sb.AppendLine($"- Parent Manifest Version: {report.Manifest.Metadata.ParentManifestVersion}");

            sb.AppendLine($"- Service Count: {report.Manifest.Services.Count}");
            sb.AppendLine($"- Datastore Count: {report.Manifest.Datastores.Count}");
            sb.AppendLine($"- Relationship Count: {report.Manifest.Relationships.Count}");
            sb.AppendLine();

            if (report.Manifest.Services.Count > 0)
            {
                sb.AppendLine("### Services");
                sb.AppendLine();

                foreach (ManifestService service in report.Manifest.Services.OrderBy(x => x.ServiceName))
                {
                    sb.AppendLine($"- **{service.ServiceName}**");
                    sb.AppendLine($"  - Type: {service.ServiceType}");
                    sb.AppendLine($"  - Platform: {service.RuntimePlatform}");

                    if (!string.IsNullOrWhiteSpace(service.Purpose))

                        sb.AppendLine($"  - Purpose: {service.Purpose}");

                    if (service.RequiredControls.Count > 0)

                        sb.AppendLine($"  - Required Controls: {string.Join(", ", service.RequiredControls)}");
                }

                sb.AppendLine();
            }

            if (report.Manifest.Datastores.Count > 0)
            {
                sb.AppendLine("### Datastores");
                sb.AppendLine();

                foreach (ManifestDatastore datastore in report.Manifest.Datastores.OrderBy(x => x.DatastoreName))
                {
                    sb.AppendLine($"- **{datastore.DatastoreName}**");
                    sb.AppendLine($"  - Type: {datastore.DatastoreType}");
                    sb.AppendLine($"  - Platform: {datastore.RuntimePlatform}");
                    sb.AppendLine(
                        $"  - Private Endpoint Required: {(datastore.PrivateEndpointRequired ? "Yes" : "No")}");
                    sb.AppendLine(
                        $"  - Encryption At Rest Required: {(datastore.EncryptionAtRestRequired ? "Yes" : "No")}");
                }

                sb.AppendLine();
            }

            if (report.Manifest.Governance.RequiredControls.Count > 0
                || report.Manifest.Governance.ComplianceTags.Count > 0
                || report.Manifest.Governance.PolicyConstraints.Count > 0)
            {
                sb.AppendLine("### Governance");
                sb.AppendLine();
                sb.AppendLine($"- Required Controls: {string.Join(", ", report.Manifest.Governance.RequiredControls)}");
                sb.AppendLine($"- Compliance Tags: {string.Join(", ", report.Manifest.Governance.ComplianceTags)}");
                sb.AppendLine(
                    $"- Policy Constraints: {string.Join(", ", report.Manifest.Governance.PolicyConstraints)}");
                sb.AppendLine($"- Risk Classification: {report.Manifest.Governance.RiskClassification}");
                sb.AppendLine($"- Cost Classification: {report.Manifest.Governance.CostClassification}");
                sb.AppendLine();
            }
        }

        if (!string.IsNullOrWhiteSpace(report.Diagram))
        {
            sb.AppendLine("## Diagram");
            sb.AppendLine();
            sb.AppendLine("```mermaid");
            sb.AppendLine(report.Diagram);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(report.Summary))
        {
            sb.AppendLine("## Architecture Summary");
            sb.AppendLine();
            sb.AppendLine(report.Summary.Trim());
            sb.AppendLine();
        }

        if (report.Determinism is not null)
        {
            sb.AppendLine("## Determinism Check");
            sb.AppendLine();
            sb.AppendLine($"- Source Run ID: {report.Determinism.SourceRunId}");
            sb.AppendLine($"- Iterations: {report.Determinism.Iterations}");
            sb.AppendLine($"- Execution Mode: {report.Determinism.ExecutionMode}");
            sb.AppendLine($"- Is Deterministic: {(report.Determinism.IsDeterministic ? "Yes" : "No")}");
            sb.AppendLine($"- Baseline Replay Run ID: {report.Determinism.BaselineReplayRunId}");
            sb.AppendLine();

            foreach (DeterminismIterationResult iteration in report.Determinism.IterationResults.OrderBy(x =>
                         x.IterationNumber))
            {
                sb.AppendLine($"### Iteration {iteration.IterationNumber}");
                sb.AppendLine();
                sb.AppendLine($"- Replay Run ID: {iteration.ReplayRunId}");
                sb.AppendLine(
                    $"- Matches Baseline Agent Results: {(iteration.MatchesBaselineAgentResults ? "Yes" : "No")}");
                sb.AppendLine($"- Matches Baseline Manifest: {(iteration.MatchesBaselineManifest ? "Yes" : "No")}");

                if (iteration.AgentDriftWarnings.Count > 0)
                {
                    sb.AppendLine("- Agent Drift Warnings:");
                    foreach (string warning in iteration.AgentDriftWarnings)

                        sb.AppendLine($"  - {warning}");
                }

                if (iteration.ManifestDriftWarnings.Count > 0)
                {
                    sb.AppendLine("- Manifest Drift Warnings:");
                    foreach (string warning in iteration.ManifestDriftWarnings)

                        sb.AppendLine($"  - {warning}");
                }

                sb.AppendLine();
            }
        }

        if (report.ManifestDiff is not null)
        {
            sb.AppendLine("## Manifest Diff");
            sb.AppendLine();
            AppendList(sb, "Added Services", report.ManifestDiff.AddedServices);
            AppendList(sb, "Removed Services", report.ManifestDiff.RemovedServices);
            AppendList(sb, "Added Datastores", report.ManifestDiff.AddedDatastores);
            AppendList(sb, "Removed Datastores", report.ManifestDiff.RemovedDatastores);
            AppendList(sb, "Added Required Controls", report.ManifestDiff.AddedRequiredControls);
            AppendList(sb, "Removed Required Controls", report.ManifestDiff.RemovedRequiredControls);

            if (report.ManifestDiff.Warnings.Count > 0)

                AppendList(sb, "Warnings", report.ManifestDiff.Warnings);
        }

        if (report.AgentResultDiff is null)
            return sb.ToString();

        sb.AppendLine("## Agent Result Diff");
        sb.AppendLine();

        foreach (AgentResultDelta delta in report.AgentResultDiff.AgentDeltas.OrderBy(x => x.AgentType))
        {
            sb.AppendLine($"### {delta.AgentType}");
            sb.AppendLine();
            sb.AppendLine($"- Left Exists: {(delta.LeftExists ? "Yes" : "No")}");
            sb.AppendLine($"- Right Exists: {(delta.RightExists ? "Yes" : "No")}");
            sb.AppendLine(
                $"- Left Confidence: {(delta.LeftConfidence.HasValue ? delta.LeftConfidence.Value.ToString("0.00") : "n/a")}");
            sb.AppendLine(
                $"- Right Confidence: {(delta.RightConfidence.HasValue ? delta.RightConfidence.Value.ToString("0.00") : "n/a")}");
            sb.AppendLine();

            AppendList(sb, "Added Claims", delta.AddedClaims);
            AppendList(sb, "Removed Claims", delta.RemovedClaims);
            AppendList(sb, "Added Evidence References", delta.AddedEvidenceRefs);
            AppendList(sb, "Removed Evidence References", delta.RemovedEvidenceRefs);
            AppendList(sb, "Added Findings", delta.AddedFindings);
            AppendList(sb, "Removed Findings", delta.RemovedFindings);
            AppendList(sb, "Added Required Controls", delta.AddedRequiredControls);
            AppendList(sb, "Removed Required Controls", delta.RemovedRequiredControls);
            AppendList(sb, "Added Warnings", delta.AddedWarnings);
            AppendList(sb, "Removed Warnings", delta.RemovedWarnings);
        }

        return sb.ToString();
    }

    private static void AppendList(StringBuilder sb, string title, IReadOnlyCollection<string> items)
    {
        sb.AppendLine($"#### {title}");
        sb.AppendLine();

        if (items.Count == 0)
        {
            sb.AppendLine("- None");
            sb.AppendLine();
            return;
        }

        foreach (string item in items)

            sb.AppendLine($"- {item}");

        sb.AppendLine();
    }
}
