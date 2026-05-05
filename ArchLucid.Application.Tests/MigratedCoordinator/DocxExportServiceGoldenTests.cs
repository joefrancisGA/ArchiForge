using ArchLucid.Application.Diagrams;
using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.ArtifactSynthesis.Docx.Models;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Core.Diagrams;
using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Advisory.Services;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;

using DocumentFormat.OpenXml.Packaging;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.MigratedCoordinator;

/// <summary>
/// Golden-style checks on <see cref="DocxExportService"/> output: valid OpenXML package and stable anchor strings from a minimal manifest.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Slow")]
public sealed class DocxExportServiceGoldenTests
{
    [SkippableFact]
    public async Task ExportAsync_produces_valid_docx_with_title_and_stable_summary_anchor()
    {
        Guid runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid manifestId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        ManifestDocument manifest = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc),
            ManifestHash = "golden-hash",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
            Metadata = new ManifestMetadata
            {
                Name = "Golden manifest",
                Summary = "GOLDEN_DOCX_SUMMARY_ANCHOR â€” deterministic advisory blurb for snapshot tests.",
                Version = "1.0.0",
                Status = "Resolved"
            }
        };

        Mock<IImprovementAdvisorService> advisor = new();
        advisor
            .Setup(x => x.GeneratePlanAsync(
                It.IsAny<ManifestDocument>(),
                It.IsAny<FindingsSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ImprovementPlan { RunId = runId, Recommendations = [], SummaryNotes = ["Golden plan note."] });

        DocxExportService sut = new(advisor.Object, new NullDiagramImageRenderer());

        DocxExportRequest request = new()
        {
            RunId = runId,
            ManifestId = manifestId,
            DocumentTitle = "Golden Architecture Export",
            Subtitle = "Snapshot subtitle",
            IncludeArchitectureDiagram = false,
            IncludeArtifactsAppendix = true,
            IncludeComplianceSection = true,
            IncludeCoverageSection = true,
            IncludeIssuesSection = true
        };

        DocxExportResult result = await sut.ExportAsync(request, manifest, [], CancellationToken.None);

        result.Content.Should().NotBeNullOrEmpty();
        result.FileName.Should().Contain(manifestId.ToString("N"), Exactly.Once());

        using MemoryStream wordStream = new(result.Content);
        using WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordStream, false);
        MainDocumentPart? main = wordDoc.MainDocumentPart;
        main.Should().NotBeNull();
        string xml = main.Document.OuterXml;

        xml.Should().Contain("Golden Architecture Export");
        xml.Should().Contain("GOLDEN_DOCX_SUMMARY_ANCHOR");
        xml.Should().Contain(runId.ToString());
        xml.Should().Contain(manifestId.ToString());
    }

    [SkippableFact]
    public async Task ExportAsync_embeds_mermaid_source_when_diagram_enabled_and_mermaid_artifact_present()
    {
        Guid runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid manifestId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        ManifestDocument manifest = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc),
            ManifestHash = "golden-hash",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
            Metadata = new ManifestMetadata { Name = "Golden manifest", Summary = "Summary", Version = "1.0.0", Status = "Resolved" }
        };

        Mock<IImprovementAdvisorService> advisor = new();
        advisor
            .Setup(x => x.GeneratePlanAsync(
                It.IsAny<ManifestDocument>(),
                It.IsAny<FindingsSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImprovementPlan { RunId = runId, Recommendations = [], SummaryNotes = [] });

        DocxExportService sut = new(advisor.Object, new NullDiagramImageRenderer());

        DocxExportRequest request = new()
        {
            RunId = runId,
            ManifestId = manifestId,
            DocumentTitle = "Golden Architecture Export",
            Subtitle = "Snapshot subtitle",
            IncludeArchitectureDiagram = true,
            IncludeArtifactsAppendix = false,
            IncludeComplianceSection = false,
            IncludeCoverageSection = false,
            IncludeIssuesSection = false
        };

        List<SynthesizedArtifact> artifacts =
        [
            new()
            {
                ArtifactId = Guid.NewGuid(),
                RunId = runId,
                ManifestId = manifestId,
                CreatedUtc = DateTime.UtcNow,
                ArtifactType = ArtifactType.MermaidDiagram,
                Name = "architecture.mmd",
                Format = "mermaid",
                Content = "flowchart TD\n    n1[\"Node\"]",
                ContentHash = "h",
            }
        ];

        DocxExportResult result = await sut.ExportAsync(request, manifest, artifacts, CancellationToken.None);

        using MemoryStream wordStream = new(result.Content);
        using WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordStream, false);
        MainDocumentPart? main = wordDoc.MainDocumentPart;
        main.Should().NotBeNull();
        string xml = main.Document.OuterXml;
        xml.Should().Contain("flowchart TD");
    }

    [SkippableFact]
    public async Task ExportAsync_embeds_drawing_blip_when_mermaid_renderer_returns_png_bytes()
    {
        Guid runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid manifestId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        ManifestDocument manifest = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc),
            ManifestHash = "golden-hash",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
            Metadata = new ManifestMetadata { Name = "Golden manifest", Summary = "Summary", Version = "1.0.0", Status = "Resolved" }
        };

        Mock<IImprovementAdvisorService> advisor = new();
        advisor
            .Setup(x => x.GeneratePlanAsync(
                It.IsAny<ManifestDocument>(),
                It.IsAny<FindingsSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImprovementPlan { RunId = runId, Recommendations = [], SummaryNotes = [] });

        byte[] tinyPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        Mock<IDiagramImageRenderer> diagram = new();
        diagram
            .Setup(x => x.RenderMermaidPngAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tinyPng);

        DocxExportService sut = new(advisor.Object, diagram.Object);

        DocxExportRequest request = new()
        {
            RunId = runId,
            ManifestId = manifestId,
            DocumentTitle = "Golden Architecture Export",
            Subtitle = "Snapshot subtitle",
            IncludeArchitectureDiagram = true,
            IncludeArtifactsAppendix = false,
            IncludeComplianceSection = false,
            IncludeCoverageSection = false,
            IncludeIssuesSection = false
        };

        List<SynthesizedArtifact> artifacts =
        [
            new()
            {
                ArtifactId = Guid.NewGuid(),
                RunId = runId,
                ManifestId = manifestId,
                CreatedUtc = DateTime.UtcNow,
                ArtifactType = ArtifactType.MermaidDiagram,
                Name = "architecture.mmd",
                Format = "mermaid",
                Content = "flowchart TD\n    n1[\"Node\"]",
                ContentHash = "h",
            }
        ];

        DocxExportResult result = await sut.ExportAsync(request, manifest, artifacts, CancellationToken.None);

        using MemoryStream wordStream = new(result.Content);
        using WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordStream, false);
        MainDocumentPart? main = wordDoc.MainDocumentPart;
        main.Should().NotBeNull();
        string xml = main.Document.OuterXml;
        xml.Should().Contain("blip");

        diagram.Verify(
            x => x.RenderMermaidPngAsync(
                It.Is<string>(s => s.Contains("flowchart TD", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
