using ArchLucid.Application.Analysis;
using ArchLucid.Application.Exports;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;

using FluentAssertions;

namespace ArchLucid.Application.Tests;

public sealed class MarkdownExportFormattingTests
{
    [Fact]
    public void MarkdownDriftReportFormatter_FormatMarkdown_includes_drift_and_table_rows()
    {
        MarkdownDriftReportFormatter fmt = new();
        DriftAnalysisResult drift = new()
        {
            DriftDetected = true,
            Summary = "S",
            Items =
            [
                new DriftItem
                {
                    Category = "C",
                    Path = "p|ipe",
                    StoredValue = "old",
                    RegeneratedValue = "new",
                    Description = "d\nline",
                },
            ],
        };

        string md = fmt.FormatMarkdown(drift, "rec-1");

        md.Should().Contain("# ArchLucid Comparison Drift Report");
        md.Should().Contain("`rec-1`");
        md.Should().Contain("Drift detected:** Yes");
        md.Should().Contain("## Differences");
        md.Should().Contain("| C |");
        md.Should().Contain("d line");
    }

    [Fact]
    public void MarkdownDriftReportFormatter_FormatHtml_encodes_comparison_id()
    {
        MarkdownDriftReportFormatter fmt = new();
        DriftAnalysisResult drift = new() { DriftDetected = false };

        string html = fmt.FormatHtml(drift, "<b>x</b>");

        html.Should().Contain("&lt;b&gt;x&lt;/b&gt;");
    }

    [Fact]
    public void MarkdownExportRecordDiffSummaryFormatter_lists_sections()
    {
        MarkdownExportRecordDiffSummaryFormatter fmt = new();
        ExportRecordDiffResult diff = new()
        {
            LeftExportRecordId = "L",
            RightExportRecordId = "R",
            LeftRunId = "lr",
            RightRunId = "rr",
            ChangedTopLevelFields = ["a"],
            RequestDiff = new ExportRecordRequestDiff
            {
                ChangedFlags = [],
                ChangedValues = ["v"],
            },
            Warnings = ["w"],
        };

        string md = fmt.FormatMarkdown(diff);

        md.Should().Contain("L -> R");
        md.Should().Contain("## Changed Top-Level Fields");
        md.Should().Contain("## Warnings");
    }

    [Fact]
    public void MarkdownArchitectureExportService_includes_diagram_fence_and_evidence_counts()
    {
        MarkdownArchitectureExportService svc = new();
        GoldenManifest m = new() { SystemName = "Sys" };
        AgentEvidencePackage ev = new()
        {
            EvidencePackageId = "e1",
            RunId = "r1",
            RequestId = "q1",
            Policies = [new PolicyEvidence()],
            ServiceCatalog = [new ServiceCatalogEvidence()],
            Patterns = [new PatternEvidence()],
            Notes = [new EvidenceNote()],
        };

        string md = svc.GenerateMarkdownPackage(m, "graph TD\nA-->B", "Summary **text**", ev);

        md.Should().Contain("# Architecture Export: Sys");
        md.Should().Contain("```mermaid");
        md.Should().Contain("Summary **text**");
        md.Should().Contain("Evidence Package Snapshot");
        md.Should().Contain("Policy Count: 1");
    }
}
