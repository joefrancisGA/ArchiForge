using ArchiForge.ArtifactSynthesis.Docx.Builders;
using ArchiForge.ArtifactSynthesis.Docx.Helpers;
using ArchiForge.ArtifactSynthesis.Docx.Models;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Explanation;
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
        using var stream = TemplateLoader.OpenWritableTemplate();

        using (var doc = WordprocessingDocument.Open(stream, true))
        {
            var main = doc.MainDocumentPart ?? throw new InvalidOperationException("Invalid template: missing main document part.");
            var body = main.Document?.Body ?? throw new InvalidOperationException("Invalid template: missing body.");

            var sectPr = body.Elements<SectionProperties>().LastOrDefault();
            sectPr?.Remove();

            foreach (var child in body.ChildElements.ToList())
                child.Remove();

            BuildDocument(doc, body, request, manifest, artifacts);

            if (sectPr is not null)
                body.AppendChild(sectPr);
            else
            {
                body.AppendChild(
                    new SectionProperties(
                        new PageSize { Width = 12240U, Height = 15840U },
                        new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 }));
            }

            doc.Save();
        }

        return Task.FromResult(new DocxExportResult
        {
            FileName = $"archiforge-architecture-package-{manifest.ManifestId:N}.docx",
            Content = stream.ToArray()
        });
    }

    private static void BuildDocument(
        WordprocessingDocument doc,
        Body body,
        DocxExportRequest request,
        GoldenManifest manifest,
        IReadOnlyList<SynthesizedArtifact> artifacts)
    {
        WordDocumentBuilder.AddStyledParagraph(body, request.DocumentTitle, DocxStyleIds.Title);
        WordDocumentBuilder.AddBodyText(body, request.Subtitle);
        WordDocumentBuilder.AddSpacer(body);
        WordDocumentBuilder.AddBodyText(body, $"Run ID: {manifest.RunId}");
        WordDocumentBuilder.AddBodyText(body, $"Manifest ID: {manifest.ManifestId}");
        WordDocumentBuilder.AddBodyText(body, $"Generated: {manifest.CreatedUtc:u}");
        WordDocumentBuilder.AddSpacer(body, 2);

        WordDocumentBuilder.AddHeading(body, "Executive Summary", DocxStyleIds.Heading1);
        if (string.IsNullOrWhiteSpace(manifest.Metadata.Summary))
            WordDocumentBuilder.AddBodyText(body, "No summary was recorded for this manifest.");
        else
            WordDocumentBuilder.AddMultilineBodyText(body, manifest.Metadata.Summary);

        WordDocumentBuilder.AddSpacer(body);

        if (request.RunExplanation is not null)
            AppendRunExplanation(body, request.RunExplanation);

        if (request.IncludeArchitectureDiagram)
        {
            WordDocumentBuilder.AddHeading(body, "Architecture Diagram", DocxStyleIds.Heading1);
            ImageHelper.AddPngToBody(
                doc,
                body,
                DiagramPlaceholderBytes.Png.ToArray(),
                "Architecture overview (placeholder)",
                ImageHelper.DefaultDiagramWidthEmu,
                ImageHelper.DefaultDiagramHeightEmu);
            WordDocumentBuilder.AddBodyText(
                body,
                "Placeholder image — future releases can embed Mermaid renders or knowledge-graph snapshots.");
            WordDocumentBuilder.AddSpacer(body, 2);
        }

        if (request.IncludeCoverageSection)
        {
            WordDocumentBuilder.AddHeading(body, "Requirements Coverage", DocxStyleIds.Heading1);
            var reqRows = new List<(string Name, string Status, string Mandatory)>();
            foreach (var item in manifest.Requirements.Covered)
                reqRows.Add((item.RequirementName, item.CoverageStatus, item.IsMandatory ? "Yes" : "No"));
            foreach (var item in manifest.Requirements.Uncovered)
                reqRows.Add((item.RequirementName, item.CoverageStatus, item.IsMandatory ? "Yes" : "No"));

            if (reqRows.Count == 0)
                WordDocumentBuilder.AddBodyText(body, "No requirements were recorded.");
            else
                WordDocumentBuilder.AddThreeColumnTable(
                    body,
                    reqRows,
                    ("Requirement", "Coverage", "Mandatory"));
            WordDocumentBuilder.AddSpacer(body);
        }

        WordDocumentBuilder.AddHeading(body, "Topology Posture", DocxStyleIds.Heading1);
        if (manifest.Topology.Resources.Count > 0)
        {
            foreach (var resource in manifest.Topology.Resources)
                WordDocumentBuilder.AddBodyText(body, $"Resource: {resource}");
        }
        else
            WordDocumentBuilder.AddBodyText(body, "No concrete topology resources were recorded.");

        if (manifest.Topology.SelectedPatterns.Count > 0)
        {
            WordDocumentBuilder.AddBodyText(body, "Selected patterns:");
            WordDocumentBuilder.AddBulletList(body, manifest.Topology.SelectedPatterns);
        }

        foreach (var gap in manifest.Topology.Gaps)
            WordDocumentBuilder.AddBodyText(body, $"Gap: {gap}");
        WordDocumentBuilder.AddSpacer(body);

        WordDocumentBuilder.AddHeading(body, "Security Posture", DocxStyleIds.Heading1);
        if (manifest.Security.Controls.Count == 0)
        {
            WordDocumentBuilder.AddBodyText(body, "No security controls were recorded.");
        }
        else
        {
            var secRows = manifest.Security.Controls
                .Select(c => (c.ControlId, c.ControlName, c.Status, c.Impact ?? string.Empty))
                .ToList();
            WordDocumentBuilder.AddFourColumnTable(
                body,
                ("Control ID", "Control", "Status", "Impact"),
                secRows);
        }

        foreach (var gap in manifest.Security.Gaps)
            WordDocumentBuilder.AddBodyText(body, $"Security gap: {gap}");
        WordDocumentBuilder.AddSpacer(body);

        if (request.IncludeComplianceSection)
        {
            WordDocumentBuilder.AddHeading(body, "Compliance Posture", DocxStyleIds.Heading1);
            if (manifest.Compliance.Controls.Count == 0)
            {
                WordDocumentBuilder.AddBodyText(body, "No compliance posture items were recorded.");
            }
            else
            {
                var compRows = manifest.Compliance.Controls
                    .Select(c => (c.ControlId, c.ControlName, c.AppliesToCategory ?? string.Empty, c.Status))
                    .ToList();
                WordDocumentBuilder.AddFourColumnTable(
                    body,
                    ("Control ID", "Control", "Category", "Status"),
                    compRows);
            }

            foreach (var gap in manifest.Compliance.Gaps)
                WordDocumentBuilder.AddBodyText(body, $"Compliance gap: {gap}");
            WordDocumentBuilder.AddSpacer(body);
        }

        WordDocumentBuilder.AddHeading(body, "Cost Posture", DocxStyleIds.Heading1);
        WordDocumentBuilder.AddBodyText(
            body,
            $"Max monthly cost: {(manifest.Cost.MaxMonthlyCost.HasValue ? manifest.Cost.MaxMonthlyCost.Value.ToString("0.00") : "Not specified")}");

        foreach (var risk in manifest.Cost.CostRisks)
            WordDocumentBuilder.AddBodyText(body, $"Cost risk: {risk}");

        foreach (var note in manifest.Cost.Notes)
            WordDocumentBuilder.AddBodyText(body, $"Cost note: {note}");
        WordDocumentBuilder.AddSpacer(body);

        if (request.IncludeIssuesSection)
        {
            WordDocumentBuilder.AddHeading(body, "Unresolved Issues", DocxStyleIds.Heading1);
            if (manifest.UnresolvedIssues.Items.Count == 0)
                WordDocumentBuilder.AddBodyText(body, "No unresolved issues.");
            else
                WordDocumentBuilder.AddIssuesTable(body, manifest.UnresolvedIssues.Items);
            WordDocumentBuilder.AddSpacer(body);
        }

        WordDocumentBuilder.AddHeading(body, "Decisions", DocxStyleIds.Heading1);
        if (manifest.Decisions.Count == 0)
        {
            WordDocumentBuilder.AddBodyText(body, "No decisions recorded.");
        }
        else
        {
            var decRows = manifest.Decisions
                .Select(d => (d.Category, d.Title, d.SelectedOption))
                .ToList();
            WordDocumentBuilder.AddThreeColumnTable(
                body,
                decRows,
                ("Category", "Decision", "Selected option"));
        }

        WordDocumentBuilder.AddSpacer(body);

        if (request.ManifestComparison is not null)
            AppendManifestComparison(body, request.ManifestComparison);

        if (request.ComparisonExplanation is not null)
            AppendComparisonExplanation(body, request.ComparisonExplanation);

        if (request.IncludeArtifactsAppendix)
        {
            WordDocumentBuilder.AddHeading(body, "Appendix A — Artifacts", DocxStyleIds.Heading1);
            if (artifacts.Count == 0)
            {
                WordDocumentBuilder.AddBodyText(body, "No synthesized artifacts were available.");
            }
            else
            {
                var artRows = artifacts
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(a => (a.Name, a.ArtifactType, a.Format))
                    .ToList();
                WordDocumentBuilder.AddThreeColumnTable(
                    body,
                    artRows,
                    ("Name", "Type", "Format"));
            }

            WordDocumentBuilder.AddSpacer(body);
        }

        WordDocumentBuilder.AddHeading(body, "Appendix B — Provenance Summary", DocxStyleIds.Heading1);
        WordDocumentBuilder.AddSimpleTable(
            body,
            [
                ("Metric", "Value"),
                ("Rule set", $"{manifest.RuleSetId} {manifest.RuleSetVersion}"),
                ("Manifest hash", manifest.ManifestHash),
                ("Source findings", manifest.Provenance.SourceFindingIds.Count.ToString()),
                ("Source graph nodes", manifest.Provenance.SourceGraphNodeIds.Count.ToString()),
                ("Applied rules", manifest.Provenance.AppliedRuleIds.Count.ToString())
            ],
            headerRow: true);
    }

    private static void AppendManifestComparison(Body body, ComparisonResult c)
    {
        WordDocumentBuilder.AddHeading(body, "Architecture Comparison", DocxStyleIds.Heading1);
        WordDocumentBuilder.AddBodyText(
            body,
            $"Base run: {c.BaseRunId} → Target run: {c.TargetRunId}");
        WordDocumentBuilder.AddSpacer(body);

        WordDocumentBuilder.AddHeading(body, "Summary Highlights", DocxStyleIds.Heading2);
        if (c.SummaryHighlights.Count == 0)
            WordDocumentBuilder.AddBodyText(body, "—");
        else
            WordDocumentBuilder.AddBulletList(body, c.SummaryHighlights);

        WordDocumentBuilder.AddHeading(body, "Decision Changes", DocxStyleIds.Heading2);
        if (c.DecisionChanges.Count == 0)
            WordDocumentBuilder.AddBodyText(body, "No decision changes.");
        else
        {
            foreach (var d in c.DecisionChanges)
            {
                WordDocumentBuilder.AddBodyText(
                    body,
                    $"{d.DecisionKey}: {FormatOptional(d.BaseValue)} → {FormatOptional(d.TargetValue)} ({d.ChangeType})");
            }
        }

        WordDocumentBuilder.AddHeading(body, "Requirement Changes", DocxStyleIds.Heading2);
        if (c.RequirementChanges.Count == 0)
            WordDocumentBuilder.AddBodyText(body, "No requirement changes.");
        else
        {
            foreach (var r in c.RequirementChanges)
                WordDocumentBuilder.AddBodyText(body, $"{r.RequirementName}: {r.ChangeType}");
        }

        WordDocumentBuilder.AddHeading(body, "Security Posture Delta", DocxStyleIds.Heading2);
        if (c.SecurityChanges.Count == 0)
            WordDocumentBuilder.AddBodyText(body, "No security control changes.");
        else
        {
            foreach (var s in c.SecurityChanges)
            {
                WordDocumentBuilder.AddBodyText(
                    body,
                    $"{s.ControlName}: {FormatOptional(s.BaseStatus)} → {FormatOptional(s.TargetStatus)}");
            }
        }

        WordDocumentBuilder.AddHeading(body, "Topology Changes", DocxStyleIds.Heading2);
        if (c.TopologyChanges.Count == 0)
            WordDocumentBuilder.AddBodyText(body, "No topology resource changes.");
        else
        {
            foreach (var t in c.TopologyChanges)
                WordDocumentBuilder.AddBodyText(body, $"{t.Resource} ({t.ChangeType})");
        }

        WordDocumentBuilder.AddHeading(body, "Cost Delta", DocxStyleIds.Heading2);
        if (c.CostChanges.Count == 0)
            WordDocumentBuilder.AddBodyText(body, "Maximum monthly cost unchanged.");
        else
        {
            foreach (var x in c.CostChanges)
            {
                WordDocumentBuilder.AddBodyText(
                    body,
                    $"{FormatCost(x.BaseCost)} → {FormatCost(x.TargetCost)}");
            }
        }

        WordDocumentBuilder.AddSpacer(body);
    }

    private static void AppendRunExplanation(Body body, ExplanationResult e)
    {
        WordDocumentBuilder.AddHeading(body, "Executive Narrative (AI)", DocxStyleIds.Heading1);
        WordDocumentBuilder.AddBodyText(body, e.Summary);
        WordDocumentBuilder.AddSpacer(body);
        WordDocumentBuilder.AddHeading(body, "Key Drivers", DocxStyleIds.Heading2);
        WordDocumentBuilder.AddBulletList(body, e.KeyDrivers.Count > 0 ? e.KeyDrivers : ["(none)"]);
        WordDocumentBuilder.AddHeading(body, "Risk Implications", DocxStyleIds.Heading2);
        WordDocumentBuilder.AddBulletList(body, e.RiskImplications.Count > 0 ? e.RiskImplications : ["(none)"]);
        WordDocumentBuilder.AddHeading(body, "Cost Implications", DocxStyleIds.Heading2);
        WordDocumentBuilder.AddBulletList(body, e.CostImplications.Count > 0 ? e.CostImplications : ["(none)"]);
        WordDocumentBuilder.AddHeading(body, "Compliance Implications", DocxStyleIds.Heading2);
        WordDocumentBuilder.AddBulletList(body, e.ComplianceImplications.Count > 0 ? e.ComplianceImplications : ["(none)"]);
        WordDocumentBuilder.AddHeading(body, "Detailed Explanation", DocxStyleIds.Heading2);
        WordDocumentBuilder.AddMultilineBodyText(body, e.DetailedNarrative);
        WordDocumentBuilder.AddSpacer(body, 2);
    }

    private static void AppendComparisonExplanation(Body body, ComparisonExplanationResult e)
    {
        WordDocumentBuilder.AddHeading(body, "Executive Change Narrative (AI)", DocxStyleIds.Heading1);
        WordDocumentBuilder.AddBodyText(body, e.HighLevelSummary);
        WordDocumentBuilder.AddSpacer(body);
        WordDocumentBuilder.AddHeading(body, "Major Changes (structured)", DocxStyleIds.Heading2);
        WordDocumentBuilder.AddBulletList(body, e.MajorChanges.Count > 0 ? e.MajorChanges : ["(none)"]);
        WordDocumentBuilder.AddHeading(body, "Key Tradeoffs (AI)", DocxStyleIds.Heading2);
        WordDocumentBuilder.AddBulletList(body, e.KeyTradeoffs.Count > 0 ? e.KeyTradeoffs : ["(none)"]);
        WordDocumentBuilder.AddHeading(body, "Detailed Explanation", DocxStyleIds.Heading2);
        WordDocumentBuilder.AddMultilineBodyText(body, e.Narrative);
        WordDocumentBuilder.AddSpacer(body, 2);
    }

    private static string FormatOptional(string? v) => string.IsNullOrEmpty(v) ? "—" : v;

    private static string FormatCost(decimal? v) => v.HasValue ? v.Value.ToString("0.00") : "—";
}
