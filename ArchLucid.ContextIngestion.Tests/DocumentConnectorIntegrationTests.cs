using ArchLucid.ContextIngestion.Canonicalization;
using ArchLucid.ContextIngestion.Connectors;
using ArchLucid.ContextIngestion.Contracts;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.ContextIngestion.Parsing;
using ArchLucid.ContextIngestion.Repositories;
using ArchLucid.ContextIngestion.Services;
using ArchLucid.ContextIngestion.Summaries;

using FluentAssertions;

namespace ArchLucid.ContextIngestion.Tests;

/// <summary>
///     Integration tests for <see cref="DocumentConnector" /> exercised through
///     <see cref="ContextIngestionService" /> with real parsers (no mocks).
///     Covers the non-API caller path where <c>ContextDocumentRequestValidator</c> does not run.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class DocumentConnectorIntegrationTests
{
    private static ContextIngestionService CreateServiceWithDocumentConnector()
    {
        IReadOnlyList<IContextDocumentParser> parsers = [new PlainTextContextDocumentParser()];
        DocumentConnector connector = new(parsers);

        return new ContextIngestionService(
            [connector],
            new CanonicalInfrastructureEnricher(),
            new CanonicalDeduplicator(),
            new InMemoryContextSnapshotRepository(),
            new DefaultContextDeltaSummaryBuilder());
    }

    [Fact]
    public async Task IngestAsync_UnsupportedContentType_ProducesWarningNotException()
    {
        ContextIngestionService sut = CreateServiceWithDocumentConnector();

        ContextIngestionRequest request = new()
        {
            RunId = Guid.NewGuid(),
            ProjectId = "proj-unsupported-doc",
            Documents =
            [
                new ContextDocumentReference
                {
                    Name = "report.pdf", ContentType = "application/pdf", Content = "binary-like content"
                }
            ]
        };

        ContextSnapshot snapshot = await sut.IngestAsync(request, CancellationToken.None);

        snapshot.Warnings.Should().ContainSingle();
        string warning = snapshot.Warnings[0];
        warning.Should().Contain("report.pdf");
        warning.Should().Contain("application/pdf");
        snapshot.CanonicalObjects.Should().BeEmpty(
            "unsupported documents should be skipped, not parsed");
    }

    [Fact]
    public async Task IngestAsync_SupportedContentType_ProducesObjectsNoWarnings()
    {
        ContextIngestionService sut = CreateServiceWithDocumentConnector();

        ContextIngestionRequest request = new()
        {
            RunId = Guid.NewGuid(),
            ProjectId = "proj-supported-doc",
            Documents =
            [
                new ContextDocumentReference
                {
                    Name = "reqs.txt", ContentType = "text/plain", Content = "REQ: must encrypt data at rest"
                }
            ]
        };

        ContextSnapshot snapshot = await sut.IngestAsync(request, CancellationToken.None);

        snapshot.Warnings.Should().BeEmpty();
        snapshot.CanonicalObjects.Should().ContainSingle()
            .Which.ObjectType.Should().Be("Requirement");
    }

    [Fact]
    public async Task IngestAsync_MixedContentTypes_SkipsUnsupportedAndParsesSupported()
    {
        ContextIngestionService sut = CreateServiceWithDocumentConnector();

        ContextIngestionRequest request = new()
        {
            RunId = Guid.NewGuid(),
            ProjectId = "proj-mixed-docs",
            Documents =
            [
                new ContextDocumentReference
                {
                    Name = "good.md", ContentType = "text/markdown", Content = "REQ: availability SLA"
                },
                new ContextDocumentReference
                {
                    Name = "bad.xlsx",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    Content = "spreadsheet bytes"
                }
            ]
        };

        ContextSnapshot snapshot = await sut.IngestAsync(request, CancellationToken.None);

        snapshot.CanonicalObjects.Should().ContainSingle()
            .Which.Name.Should().Contain("availability SLA");
        snapshot.Warnings.Should().ContainSingle()
            .Which.Should().Contain("bad.xlsx");
    }
}
