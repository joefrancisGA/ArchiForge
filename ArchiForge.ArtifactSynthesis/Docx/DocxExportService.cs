using ArchiForge.ArtifactSynthesis.Docx.Builders;
using ArchiForge.ArtifactSynthesis.Docx.Models;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ArchiForge.ArtifactSynthesis.Docx;

public sealed class DocxExportService : IDocxExportService
{
    public Task<DocxExportResult> ExportAsync(
        DocxExportRequest request,
        GoldenManifest manifest,
        IReadOnlyList<SynthesizedArtifact> artifacts,
        CancellationToken ct)
    {
        _ = ct;
        using var stream = new MemoryStream();

        using (var wordDocument = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            var body = new Body();
            mainPart.Document = new Document(body);

            BuildDocument(body, request, manifest, artifacts);

            mainPart.Document.Save();
        }

        return Task.FromResult(new DocxExportResult
        {
            FileName = $"archiforge-architecture-package-{manifest.ManifestId:N}.docx",
            Content = stream.ToArray()
        });
    }

    private static void BuildDocument(
        Body body,
        DocxExportRequest request,
        GoldenManifest manifest,
        IReadOnlyList<SynthesizedArtifact> artifacts)
    {
        WordDocumentBuilder.AddHeading(body, request.DocumentTitle, "Title");
        WordDocumentBuilder.AddParagraph(body, request.Subtitle);
        WordDocumentBuilder.AddParagraph(body, $"Run ID: {manifest.RunId}");
        WordDocumentBuilder.AddParagraph(body, $"Manifest ID: {manifest.ManifestId}");
        WordDocumentBuilder.AddParagraph(body, $"Generated: {manifest.CreatedUtc:u}");

        WordDocumentBuilder.AddHeading(body, "Executive Summary");
        WordDocumentBuilder.AddParagraph(body, string.IsNullOrWhiteSpace(manifest.Metadata.Summary)
            ? "No summary was recorded for this manifest."
            : manifest.Metadata.Summary);

        if (request.IncludeCoverageSection)
        {
            WordDocumentBuilder.AddHeading(body, "Requirements Coverage");
            if (manifest.Requirements.Covered.Count == 0 && manifest.Requirements.Uncovered.Count == 0)
            {
                WordDocumentBuilder.AddParagraph(body, "No requirements were recorded.");
            }
            else
            {
                foreach (var item in manifest.Requirements.Covered)
                    WordDocumentBuilder.AddParagraph(body, $"Covered: {item.RequirementName}");

                foreach (var item in manifest.Requirements.Uncovered)
                    WordDocumentBuilder.AddParagraph(body, $"Uncovered: {item.RequirementName}");
            }
        }

        WordDocumentBuilder.AddHeading(body, "Topology Posture");
        if (manifest.Topology.Resources.Count > 0)
        {
            foreach (var resource in manifest.Topology.Resources)
                WordDocumentBuilder.AddParagraph(body, $"Resource: {resource}");
        }
        else
        {
            WordDocumentBuilder.AddParagraph(body, "No concrete topology resources were recorded.");
        }

        if (manifest.Topology.SelectedPatterns.Count > 0)
        {
            WordDocumentBuilder.AddParagraph(body, "Selected patterns:");
            WordDocumentBuilder.AddBulletList(body, manifest.Topology.SelectedPatterns);
        }

        foreach (var gap in manifest.Topology.Gaps)
            WordDocumentBuilder.AddParagraph(body, $"Gap: {gap}");

        WordDocumentBuilder.AddHeading(body, "Security Posture");
        if (manifest.Security.Controls.Count == 0)
        {
            WordDocumentBuilder.AddParagraph(body, "No security controls were recorded.");
        }
        else
        {
            foreach (var control in manifest.Security.Controls)
                WordDocumentBuilder.AddParagraph(body, $"{control.ControlName}: {control.Status}");
        }

        foreach (var gap in manifest.Security.Gaps)
            WordDocumentBuilder.AddParagraph(body, $"Security Gap: {gap}");

        if (request.IncludeComplianceSection)
        {
            WordDocumentBuilder.AddHeading(body, "Compliance Posture");
            if (manifest.Compliance.Controls.Count == 0)
            {
                WordDocumentBuilder.AddParagraph(body, "No compliance posture items were recorded.");
            }
            else
            {
                foreach (var control in manifest.Compliance.Controls)
                {
                    WordDocumentBuilder.AddParagraph(
                        body,
                        $"{control.ControlId} {control.ControlName}: {control.Status}");
                }
            }

            foreach (var gap in manifest.Compliance.Gaps)
                WordDocumentBuilder.AddParagraph(body, $"Compliance Gap: {gap}");
        }

        WordDocumentBuilder.AddHeading(body, "Cost Posture");
        WordDocumentBuilder.AddParagraph(
            body,
            $"Max Monthly Cost: {(manifest.Cost.MaxMonthlyCost.HasValue ? manifest.Cost.MaxMonthlyCost.Value.ToString("0.00") : "Not specified")}");

        foreach (var risk in manifest.Cost.CostRisks)
            WordDocumentBuilder.AddParagraph(body, $"Cost Risk: {risk}");

        foreach (var note in manifest.Cost.Notes)
            WordDocumentBuilder.AddParagraph(body, $"Cost Note: {note}");

        if (request.IncludeIssuesSection)
        {
            WordDocumentBuilder.AddHeading(body, "Unresolved Issues");
            if (manifest.UnresolvedIssues.Items.Count == 0)
            {
                WordDocumentBuilder.AddParagraph(body, "No unresolved issues.");
            }
            else
            {
                foreach (var issue in manifest.UnresolvedIssues.Items)
                {
                    WordDocumentBuilder.AddParagraph(
                        body,
                        $"[{issue.Severity}] {issue.Title}: {issue.Description}");
                }
            }
        }

        WordDocumentBuilder.AddHeading(body, "Decisions");
        if (manifest.Decisions.Count == 0)
        {
            WordDocumentBuilder.AddParagraph(body, "No decisions recorded.");
        }
        else
        {
            foreach (var decision in manifest.Decisions)
            {
                WordDocumentBuilder.AddParagraph(
                    body,
                    $"{decision.Category}: {decision.Title} -> {decision.SelectedOption}");
            }
        }

        if (request.IncludeArtifactsAppendix)
        {
            WordDocumentBuilder.AddHeading(body, "Appendix A - Artifacts");
            if (artifacts.Count == 0)
            {
                WordDocumentBuilder.AddParagraph(body, "No synthesized artifacts were available.");
            }
            else
            {
                foreach (var artifact in artifacts.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    WordDocumentBuilder.AddParagraph(
                        body,
                        $"{artifact.Name} ({artifact.ArtifactType}, {artifact.Format})");
                }
            }
        }

        WordDocumentBuilder.AddHeading(body, "Appendix B - Provenance Summary");
        WordDocumentBuilder.AddSimpleTable(body,
        [
            ("Rule Set", $"{manifest.RuleSetId} {manifest.RuleSetVersion}"),
            ("Manifest Hash", manifest.ManifestHash),
            ("Source Findings", manifest.Provenance.SourceFindingIds.Count.ToString()),
            ("Source Graph Nodes", manifest.Provenance.SourceGraphNodeIds.Count.ToString()),
            ("Applied Rules", manifest.Provenance.AppliedRuleIds.Count.ToString())
        ]);
    }
}
