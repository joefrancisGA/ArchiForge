using ArchiForge.ArtifactSynthesis.Docx.Builders;
using ArchiForge.ArtifactSynthesis.Docx.Helpers;
using ArchiForge.ArtifactSynthesis.Docx.Models;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Explanation;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ArchiForge.ArtifactSynthesis.Docx;

/// <summary>
/// <see cref="IDocxExportService"/> implementation using embedded template, <see cref="IImprovementAdvisorService"/> for advisory sections, and OpenXML builders.
/// </summary>
public sealed class DocxExportService(IImprovementAdvisorService improvementAdvisorService) : IDocxExportService
{
    /// <inheritdoc />
    public async Task<DocxExportResult> ExportAsync(
        DocxExportRequest request,
        GoldenManifest manifest,
        IReadOnlyList<SynthesizedArtifact> artifacts,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(artifacts);
        FindingsSnapshot findings = request.FindingsSnapshot ?? CreateFallbackFindings(manifest);
        ImprovementPlan improvementPlan = request.ManifestComparison is not null
            ? await improvementAdvisorService
                .GeneratePlanAsync(manifest, findings, request.ManifestComparison, ct)
                .ConfigureAwait(false)
            : await improvementAdvisorService
                .GeneratePlanAsync(manifest, findings, ct)
                .ConfigureAwait(false);

        using MemoryStream stream = TemplateLoader.OpenWritableTemplate();

        using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, true))
        {
            MainDocumentPart main = doc.MainDocumentPart ?? throw new InvalidOperationException("Invalid template: missing main document part.");
            Body body = main.Document.Body ?? throw new InvalidOperationException("Invalid template: missing body.");

            SectionProperties? sectPr = body.Elements<SectionProperties>().LastOrDefault();
            sectPr?.Remove();

            foreach (OpenXmlElement child in body.ChildElements.ToList())
                child.Remove();

            BuildDocument(doc, body, request, manifest, artifacts, improvementPlan);

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

        return new DocxExportResult
        {
            FileName = $"archiforge-architecture-package-{manifest.ManifestId:N}.docx",
            Content = stream.ToArray()
        };
    }

    /// <summary>Empty findings aligned with the manifest when the export request has no persisted snapshot.</summary>
    private static FindingsSnapshot CreateFallbackFindings(GoldenManifest manifest) =>
        new()
        {
            SchemaVersion = FindingsSchema.CurrentSnapshotVersion,
            FindingsSnapshotId = manifest.FindingsSnapshotId,
            RunId = manifest.RunId,
            ContextSnapshotId = manifest.ContextSnapshotId,
            GraphSnapshotId = manifest.GraphSnapshotId,
            CreatedUtc = manifest.CreatedUtc,
            Findings = []
        };

    private static void BuildDocument(
        WordprocessingDocument doc,
        Body body,
        DocxExportRequest request,
        GoldenManifest manifest,
        IReadOnlyList<SynthesizedArtifact> artifacts,
        ImprovementPlan improvementPlan)
    {
        WordDocumentBuilder.AddStyledParagraph(body, request.DocumentTitle, DocxStyleIds.Title);
        WordDocumentBuilder.AddBodyText(body, request.Subtitle);
        WordDocumentBuilder.AddSpacer(body);
        WordDocumentBuilder.AddBodyText(body, $"Run ID: {manifest.RunId}");
        WordDocumentBuilder.AddBodyText(body, $"Manifest ID: {manifest.ManifestId}");
        WordDocumentBuilder.AddBodyText(body, $"Generated: {manifest.CreatedUtc:u}");
        WordDocumentBuilder.AddSpacer(body, 2);

        WordDocumentBuilder.AddHeading(body, "Executive Summary");
        if (string.IsNullOrWhiteSpace(manifest.Metadata.Summary))
            WordDocumentBuilder.AddBodyText(body, "No summary was recorded for this manifest.");
        else
            WordDocumentBuilder.AddMultilineBodyText(body, manifest.Metadata.Summary);

        WordDocumentBuilder.AddSpacer(body);

        if (request.RunExplanation is not null)
            AppendRunExplanation(body, request.RunExplanation);

        if (request.IncludeArchitectureDiagram)
        {
            WordDocumentBuilder.AddHeading(body, "Architecture Diagram");
            ImageHelper.AddPngToBody(
                doc,
                body,
                DiagramPlaceholderBytes.Png.ToArray(),
                "Architecture overview (placeholder)");
            WordDocumentBuilder.AddBodyText(
                body,
                "Placeholder image — future releases can embed Mermaid renders or knowledge-graph snapshots.");
            WordDocumentBuilder.AddSpacer(body, 2);
        }

        if (request.IncludeCoverageSection)
        {
            WordDocumentBuilder.AddHeading(body, "Requirements Coverage");
            List<(string Name, string Status, string Mandatory)> reqRows = new();
            foreach (RequirementCoverageItem item in manifest.Requirements.Covered)
                reqRows.Add((item.RequirementName, item.CoverageStatus, item.IsMandatory ? "Yes" : "No"));
            foreach (RequirementCoverageItem item in manifest.Requirements.Uncovered)
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

        WordDocumentBuilder.AddHeading(body, "Topology Posture");
        if (manifest.Topology.Resources.Count > 0)
        {
            foreach (string resource in manifest.Topology.Resources)
                WordDocumentBuilder.AddBodyText(body, $"Resource: {resource}");
        }
        else
            WordDocumentBuilder.AddBodyText(body, "No concrete topology resources were recorded.");

        if (manifest.Topology.SelectedPatterns.Count > 0)
        {
            WordDocumentBuilder.AddBodyText(body, "Selected patterns:");
            WordDocumentBuilder.AddBulletList(body, manifest.Topology.SelectedPatterns);
        }

        foreach (string gap in manifest.Topology.Gaps)
            WordDocumentBuilder.AddBodyText(body, $"Gap: {gap}");
        WordDocumentBuilder.AddSpacer(body);

        WordDocumentBuilder.AddHeading(body, "Security Posture");
        if (manifest.Security.Controls.Count == 0)
        {
            WordDocumentBuilder.AddBodyText(body, "No security controls were recorded.");
        }
        else
        {
            List<(string ControlId, string ControlName, string Status, string Impact)> secRows = manifest.Security.Controls
                .Select(c => (c.ControlId, c.ControlName, c.Status, c.Impact))
                .ToList();
            WordDocumentBuilder.AddFourColumnTable(
                body,
                ("Control ID", "Control", "Status", "Impact"),
                secRows);
        }

        foreach (string gap in manifest.Security.Gaps)
            WordDocumentBuilder.AddBodyText(body, $"Security gap: {gap}");
        WordDocumentBuilder.AddSpacer(body);

        if (request.IncludeComplianceSection)
        {
            WordDocumentBuilder.AddHeading(body, "Compliance Posture");
            if (manifest.Compliance.Controls.Count == 0)
            {
                WordDocumentBuilder.AddBodyText(body, "No compliance posture items were recorded.");
            }
            else
            {
                List<(string ControlId, string ControlName, string AppliesToCategory, string Status)> compRows = manifest.Compliance.Controls
                    .Select(c => (c.ControlId, c.ControlName, c.AppliesToCategory, c.Status))
                    .ToList();
                WordDocumentBuilder.AddFourColumnTable(
                    body,
                    ("Control ID", "Control", "Category", "Status"),
                    compRows);
            }

            foreach (string gap in manifest.Compliance.Gaps)
                WordDocumentBuilder.AddBodyText(body, $"Compliance gap: {gap}");
            WordDocumentBuilder.AddSpacer(body);
        }

        WordDocumentBuilder.AddHeading(body, "Cost Posture");
        WordDocumentBuilder.AddBodyText(
            body,
            $"Max monthly cost: {(manifest.Cost.MaxMonthlyCost.HasValue ? manifest.Cost.MaxMonthlyCost.Value.ToString("0.00") : "Not specified")}");

        foreach (string risk in manifest.Cost.CostRisks)
            WordDocumentBuilder.AddBodyText(body, $"Cost risk: {risk}");

        foreach (string note in manifest.Cost.Notes)
            WordDocumentBuilder.AddBodyText(body, $"Cost note: {note}");
        WordDocumentBuilder.AddSpacer(body);

        if (request.IncludeIssuesSection)
        {
            WordDocumentBuilder.AddHeading(body, "Unresolved Issues");
            if (manifest.UnresolvedIssues.Items.Count == 0)
                WordDocumentBuilder.AddBodyText(body, "No unresolved issues.");
            else
                WordDocumentBuilder.AddIssuesTable(body, manifest.UnresolvedIssues.Items);
            WordDocumentBuilder.AddSpacer(body);
        }

        WordDocumentBuilder.AddHeading(body, "Recommended Improvements");
        if (improvementPlan.Recommendations.Count == 0)
        {
            WordDocumentBuilder.AddBodyText(body, "No significant improvements were identified.");
        }
        else
        {
            foreach (ImprovementRecommendation recommendation in improvementPlan.Recommendations.Take(10))
            {
                WordDocumentBuilder.AddBodyText(body, $"{recommendation.Title} [{recommendation.Urgency}]");
                WordDocumentBuilder.AddBodyText(body, $"Rationale: {recommendation.Rationale}");
                WordDocumentBuilder.AddBodyText(body, $"Suggested Action: {recommendation.SuggestedAction}");
                WordDocumentBuilder.AddBodyText(body, $"Expected Impact: {recommendation.ExpectedImpact}");
                WordDocumentBuilder.AddSpacer(body);
            }
        }

        WordDocumentBuilder.AddSpacer(body);

        WordDocumentBuilder.AddHeading(body, "Decisions");
        if (manifest.Decisions.Count == 0)
        {
            WordDocumentBuilder.AddBodyText(body, "No decisions recorded.");
        }
        else
        {
            List<(string Category, string Title, string SelectedOption)> decRows = manifest.Decisions
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
            WordDocumentBuilder.AddHeading(body, "Appendix A — Artifacts");
            if (artifacts.Count == 0)
            {
                WordDocumentBuilder.AddBodyText(body, "No synthesized artifacts were available.");
            }
            else
            {
                List<(string Name, string ArtifactType, string Format)> artRows = artifacts
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

        WordDocumentBuilder.AddHeading(body, "Appendix B — Provenance Summary");
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
        WordDocumentBuilder.AddHeading(body, "Architecture Comparison");
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
            foreach (DecisionDelta d in c.DecisionChanges)
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
            foreach (RequirementDelta r in c.RequirementChanges)
                WordDocumentBuilder.AddBodyText(body, $"{r.RequirementName}: {r.ChangeType}");
        }

        WordDocumentBuilder.AddHeading(body, "Security Posture Delta", DocxStyleIds.Heading2);
        if (c.SecurityChanges.Count == 0)
            WordDocumentBuilder.AddBodyText(body, "No security control changes.");
        else
        {
            foreach (SecurityDelta s in c.SecurityChanges)
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
            foreach (TopologyDelta t in c.TopologyChanges)
                WordDocumentBuilder.AddBodyText(body, $"{t.Resource} ({t.ChangeType})");
        }

        WordDocumentBuilder.AddHeading(body, "Cost Delta", DocxStyleIds.Heading2);
        if (c.CostChanges.Count == 0)
            WordDocumentBuilder.AddBodyText(body, "Maximum monthly cost unchanged.");
        else
        {
            foreach (CostDelta x in c.CostChanges)
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
        WordDocumentBuilder.AddHeading(body, "Executive Narrative (AI)");
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
        WordDocumentBuilder.AddHeading(body, "Executive Change Narrative (AI)");
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
