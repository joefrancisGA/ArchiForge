using System.IO.Compression;
using System.Text;

using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.Contracts.ValueReports;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Api.Tests.ValueReports;

public sealed class DocxValueReportRendererTests
{
    [SkippableFact]
    public async Task RenderAsync_includes_key_roi_sections_in_document_xml()
    {
        DocxValueReportRenderer sut = new(NullLogger<DocxValueReportRenderer>.Instance);
        ValueReportSnapshot snapshot = new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-08T00:00:00Z"),
            [new ValueReportRunStatusRow("Completed", 2)],
            2,
            1,
            3,
            4,
            4m,
            1.5m,
            1m,
            6.5m,
            10m,
            "Test note.",
            100_000m,
            500m,
            27_360m,
            72_140m,
            263.67m,
            null,
            null,
            null,
            null,
            0,
            ReviewCycleBaselineProvenance.NoMeasurementYet,
            null,
            null,
            0,
            0,
            null,
            null);

        byte[] docx = await sut.RenderAsync(snapshot, CancellationToken.None);

        docx.Should().NotBeEmpty();

        using MemoryStream ms = new(docx);
        await using ZipArchive zip = new(ms, ZipArchiveMode.Read);
        ZipArchiveEntry? entry = zip.GetEntry("word/document.xml");
        entry.Should().NotBeNull();

        using StreamReader reader = new(await entry.OpenAsync(), Encoding.UTF8);
        string xml = await reader.ReadToEndAsync();

        xml.Should().Contain("ArchLucid \u2014 tenant value report");
        xml.Should().Contain("ROI vs ROI_MODEL.md baseline");
        xml.Should().Contain("Governance-class audit events");
        xml.Should().Contain("Drift / alert-class audit events");
        xml.Should().Contain("Per-finding feedback (thumbs)");
    }
}
