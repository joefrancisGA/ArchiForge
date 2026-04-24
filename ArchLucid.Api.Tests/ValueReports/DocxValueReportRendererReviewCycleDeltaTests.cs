using System.IO.Compression;
using System.Text;

using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.Contracts.ValueReports;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Api.Tests.ValueReports;

public sealed class DocxValueReportRendererReviewCycleDeltaTests
{
    private static ValueReportSnapshot BaseSnapshot(
        ReviewCycleBaselineProvenance provenance,
        decimal? tenantH,
        string? tenantSrc,
        DateTimeOffset? captured,
        decimal? measured,
        int sample,
        decimal? delta,
        decimal? deltaPct)
    {
        return new ValueReportSnapshot(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-08T00:00:00Z"),
            [],
            0,
            0,
            0,
            0,
            0m,
            0m,
            0m,
            0m,
            0m,
            "n",
            0m,
            0m,
            0m,
            0m,
            0m,
            tenantH,
            tenantSrc,
            captured,
            measured,
            sample,
            provenance,
            delta,
            deltaPct,
            0,
            0);
    }

    [Fact]
    public async Task RenderAsync_NoMeasurementYet_shows_heading_and_italic_stub()
    {
        DocxValueReportRenderer sut = new(NullLogger<DocxValueReportRenderer>.Instance);
        ValueReportSnapshot snap = BaseSnapshot(
            ReviewCycleBaselineProvenance.NoMeasurementYet,
            null,
            null,
            null,
            null,
            0,
            null,
            null);

        string xml = await RenderDocumentXmlAsync(sut, snap);

        xml.Should().Contain("Review-cycle delta (before vs measured)");
        xml.Should().Contain("No committed manifests in this window");
    }

    [Fact]
    public async Task RenderAsync_Defaulted_shows_safety_valve_sentence()
    {
        DocxValueReportRenderer sut = new(NullLogger<DocxValueReportRenderer>.Instance);
        ValueReportSnapshot snap = BaseSnapshot(
            ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions,
            null,
            null,
            null,
            4m,
            2,
            4m,
            50m);

        string xml = await RenderDocumentXmlAsync(sut, snap);

        xml.Should().Contain("conservative default from PILOT_ROI_MODEL.md");
        xml.Should().Contain("Numbers are illustrative, not customer-specific");
    }

    [Fact]
    public async Task RenderAsync_TenantSupplied_shows_capture_and_source_lines()
    {
        DocxValueReportRenderer sut = new(NullLogger<DocxValueReportRenderer>.Instance);
        DateTimeOffset cap = DateTimeOffset.Parse("2026-04-02T15:30:00Z");
        ValueReportSnapshot snap = BaseSnapshot(
            ReviewCycleBaselineProvenance.TenantSuppliedAtSignup,
            18m,
            "team estimate",
            cap,
            9m,
            1,
            9m,
            50m);

        string xml = await RenderDocumentXmlAsync(sut, snap);

        xml.Should().Contain("tenant-supplied at trial signup");
        xml.Should().Contain("Source note: team estimate");
        xml.Should().Contain("2026-04-02");
    }

    private static async Task<string> RenderDocumentXmlAsync(DocxValueReportRenderer sut, ValueReportSnapshot snap)
    {
        byte[] docx = await sut.RenderAsync(snap, CancellationToken.None);

        using MemoryStream ms = new(docx);
        using ZipArchive zip = new(ms, ZipArchiveMode.Read);
        ZipArchiveEntry? entry = zip.GetEntry("word/document.xml");
        entry.Should().NotBeNull();

        using StreamReader reader = new(entry.Open(), Encoding.UTF8);

        return await reader.ReadToEndAsync();
    }
}
