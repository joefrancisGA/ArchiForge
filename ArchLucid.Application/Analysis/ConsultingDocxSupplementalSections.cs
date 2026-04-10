using ArchLucid.Core.Diagrams;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ArchLucid.Application.Analysis;

/// <summary>Non-cover DOCX sections not split into dedicated named builders (TOC, executive summary, appendices, …).</summary>
internal static class ConsultingDocxSupplementalSections
{
    public static void AddDocumentControl(Body body, ArchitectureAnalysisReport report)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Document Control", 1);

        ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(
            body,
            "This document was generated from the ArchLucid analysis pipeline.",
            "BodyText");

        ConsultingDocxOpenXmlPrimitives.AddSpacer(body);

        ConsultingDocxOpenXmlPrimitives.AddKeyValueTable(body, [
            ("Document Type", "Architecture Analysis Report"),
            ("Run ID", report.Run.RunId),
            ("Request ID", report.Run.RequestId),
            ("Run Status", report.Run.Status.ToString()),
            ("Created UTC", report.Run.CreatedUtc.ToString("O")),
            ("Completed UTC", report.Run.CompletedUtc?.ToString("O") ?? "n/a"),
            ("Manifest Version", report.Run.CurrentManifestVersion ?? "n/a")
        ]);
    }

    public static void AddTableOfContentsPlaceholder(Body body)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Table of Contents", 1);
        ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(
            body,
            "Update fields in Word to refresh the table of contents.",
            "Subtle");

        ConsultingDocxOpenXmlPrimitives.AddSpacer(body);

        foreach (string item in new[]
                 {
                     "1. Executive Summary",
                     "2. Architecture Overview",
                     "3. Evidence and Constraints",
                     "4. Architecture Details",
                     "5. Governance and Controls",
                     "6. Explainability and Execution Review",
                     "7. Conclusions",
                     "Appendix A. Mermaid Source",
                     "Appendix B. Execution Trace Index",
                     "Appendix C. Determinism and Comparison"
                 })
        {
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, item);
        }
    }

    public static void AddExecutiveSummary(
        Body body,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Executive Summary", 1);

        string systemName = report.Manifest?.SystemName
                            ?? report.Evidence?.SystemName
                            ?? "the requested system";

        int serviceCount = report.Manifest?.Services.Count ?? 0;
        int datastoreCount = report.Manifest?.Datastores.Count ?? 0;
        int controlCount = report.Manifest?.Governance.RequiredControls.Count ?? 0;

        string text = options.ExecutiveSummaryTextTemplate
            .Replace("{SystemName}", systemName, StringComparison.OrdinalIgnoreCase)
            .Replace("{OrganizationName}", options.OrganizationName, StringComparison.OrdinalIgnoreCase)
            .Replace("{ServiceCount}", serviceCount.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{DatastoreCount}", datastoreCount.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{ControlCount}", controlCount.ToString(), StringComparison.OrdinalIgnoreCase);

        ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, text, "BodyText");

        if (report.Warnings.Count > 0)
        {
            ConsultingDocxOpenXmlPrimitives.AddCallout(
                body,
                "Key warnings were identified during analysis and should be reviewed before approval.",
                options);
        }
    }

    public static async Task AddArchitectureOverviewAsync(
        Body body,
        MainDocumentPart mainPart,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options,
        IDiagramImageRenderer diagramImageRenderer,
        CancellationToken cancellationToken)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Architecture Overview", 1);

        if (report.Manifest is null)
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "No manifest was available for this run.", "BodyText");

            return;
        }

        ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, options.ArchitectureOverviewIntro, "BodyText");

        if (string.IsNullOrWhiteSpace(report.Diagram))
        {
            return;
        }

        byte[]? imageBytes = await diagramImageRenderer.RenderMermaidPngAsync(
            report.Diagram,
            cancellationToken);

        if (imageBytes is not null && imageBytes.Length > 0)
        {
            ConsultingDocxOpenXmlPrimitives.AddImageToBody(
                mainPart,
                body,
                imageBytes,
                "Architecture Overview Diagram",
                6_200_000L,
                3_600_000L);
        }
        else
        {
            ConsultingDocxOpenXmlPrimitives.AddCallout(
                body,
                "Diagram image rendering was unavailable. Mermaid source is included in Appendix A.",
                options);
        }
    }

    public static void AddArchitectureDetails(Body body, ArchitectureAnalysisReport report)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Architecture Details", 1);

        if (report.Manifest is null)
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "No manifest was available for this run.", "BodyText");

            return;
        }

        if (report.Manifest.Services.Count > 0)
        {
            ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Services", 2);

            foreach (ManifestService service in report.Manifest.Services.OrderBy(x => x.ServiceName))
            {
                ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, service.ServiceName, "Strong");
                ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Type: {service.ServiceType}");
                ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Platform: {service.RuntimePlatform}");

                if (!string.IsNullOrWhiteSpace(service.Purpose))
                {
                    ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Purpose: {service.Purpose}");
                }

                if (service.RequiredControls.Count > 0)
                {
                    ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Required Controls: {string.Join(", ", service.RequiredControls)}");
                }

                ConsultingDocxOpenXmlPrimitives.AddSpacer(body);
            }
        }

        if (report.Manifest.Datastores.Count <= 0)
        {
            return;
        }

        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Datastores", 2);

        foreach (ManifestDatastore datastore in report.Manifest.Datastores.OrderBy(x => x.DatastoreName))
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, datastore.DatastoreName, "Strong");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Type: {datastore.DatastoreType}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Platform: {datastore.RuntimePlatform}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Private Endpoint Required: {(datastore.PrivateEndpointRequired ? "Yes" : "No")}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Encryption At Rest Required: {(datastore.EncryptionAtRestRequired ? "Yes" : "No")}");
            ConsultingDocxOpenXmlPrimitives.AddSpacer(body);
        }
    }

    public static void AddGovernanceAndControls(Body body, ArchitectureAnalysisReport report)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Governance and Controls", 1);

        if (report.Manifest is null)
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "No manifest was available for this run.", "BodyText");

            return;
        }

        ManifestGovernance gov = report.Manifest.Governance;

        ConsultingDocxOpenXmlPrimitives.AddKeyValueTable(body, [
            ("Risk Classification", gov.RiskClassification),
            ("Cost Classification", gov.CostClassification),
            ("Required Controls", gov.RequiredControls.Count > 0 ? string.Join(", ", gov.RequiredControls) : "None"),
            ("Compliance Tags", gov.ComplianceTags.Count > 0 ? string.Join(", ", gov.ComplianceTags) : "None"),
            ("Policy Constraints", gov.PolicyConstraints.Count > 0 ? string.Join(", ", gov.PolicyConstraints) : "None")
        ]);
    }

    public static void AddExplainabilitySection(
        Body body,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Explainability and Execution Review", 1);

        if (report.ExecutionTraces.Count == 0)
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "No execution traces were available for this run.", "BodyText");

            return;
        }

        ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(
            body,
            "This section summarizes the agent execution path and highlights the available trace information.",
            "BodyText");

        ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Execution Trace Count: {report.ExecutionTraces.Count}");

        IOrderedEnumerable<IGrouping<AgentType, AgentExecutionTrace>> grouped = report.ExecutionTraces
            .GroupBy(x => x.AgentType)
            .OrderBy(x => x.Key);

        foreach (IGrouping<AgentType, AgentExecutionTrace> group in grouped)
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, group.Key.ToString(), "Strong");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Trace Count: {group.Count()}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(
                body,
                $"Latest Parse Success: {(group.OrderByDescending(x => x.CreatedUtc).First().ParseSucceeded ? "Succeeded" : "Failed")}");
        }

        if (report.Determinism is not null)
        {
            ConsultingDocxOpenXmlPrimitives.AddSpacer(body);
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "Determinism Snapshot", "Strong");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Iterations: {report.Determinism.Iterations}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Is Deterministic: {(report.Determinism.IsDeterministic ? "Yes" : "No")}");
        }

        if (report.ManifestDiff is null && report.AgentResultDiff is null)
        {
            return;
        }

        ConsultingDocxOpenXmlPrimitives.AddSpacer(body);
        ConsultingDocxOpenXmlPrimitives.AddCallout(
            body,
            "Comparison artifacts were included in this report. See Appendix C for detail.",
            options);
    }

    public static void AddAppendices(
        Body body,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options)
    {
        if (options.IncludeAppendixMermaid)
        {
            ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Appendix A. Mermaid Source", 1);

            if (!string.IsNullOrWhiteSpace(report.Diagram))
            {
                ConsultingDocxOpenXmlPrimitives.AddCodeBlock(body, report.Diagram, ConsultingDocxOpenXmlPrimitives.MermaidLanguage);
            }
            else
            {
                ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "No Mermaid diagram source was available.", "BodyText");
            }

            ConsultingDocxOpenXmlPrimitives.AddPageBreak(body);
        }

        if (options.IncludeAppendixExecutionTraceIndex)
        {
            ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Appendix B. Execution Trace Index", 1);

            if (report.ExecutionTraces.Count > 0)
            {
                foreach (AgentExecutionTrace trace in report.ExecutionTraces.OrderBy(x => x.AgentType).ThenBy(x => x.CreatedUtc))
                {
                    ConsultingDocxOpenXmlPrimitives.AddBullet(
                        body,
                        $"{trace.AgentType} | Task {trace.TaskId} | Parse {(trace.ParseSucceeded ? "Succeeded" : "Failed")} | {trace.CreatedUtc:O}");
                }
            }
            else
            {
                ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "No execution traces were available.", "BodyText");
            }

            ConsultingDocxOpenXmlPrimitives.AddPageBreak(body);
        }

        if (!options.IncludeAppendixDeterminismAndComparison)
        {
            return;
        }

        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Appendix C. Determinism and Comparison", 1);

        if (report.Determinism is not null)
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "Determinism", "Strong");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Iterations: {report.Determinism.Iterations}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Is Deterministic: {(report.Determinism.IsDeterministic ? "Yes" : "No")}");
        }

        if (report.ManifestDiff is not null)
        {
            ConsultingDocxOpenXmlPrimitives.AddSpacer(body);
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "Manifest Diff", "Strong");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Added Services: {report.ManifestDiff.AddedServices.Count}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Removed Services: {report.ManifestDiff.RemovedServices.Count}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Added Required Controls: {report.ManifestDiff.AddedRequiredControls.Count}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Removed Required Controls: {report.ManifestDiff.RemovedRequiredControls.Count}");
        }

        if (report.AgentResultDiff is null)
        {
            return;
        }

        ConsultingDocxOpenXmlPrimitives.AddSpacer(body);
        ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "Agent Result Diff", "Strong");
        ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Agent Delta Count: {report.AgentResultDiff.AgentDeltas.Count}");
    }
}
