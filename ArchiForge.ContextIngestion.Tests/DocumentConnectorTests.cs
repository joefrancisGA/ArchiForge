using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Models;

using FluentAssertions;

using Moq;

namespace ArchiForge.ContextIngestion.Tests;

/// <summary>
/// Tests for Document Connector.
/// </summary>

public sealed class DocumentConnectorTests
{
    [Fact]
    public async Task NormalizeAsync_AddsWarning_WhenNoParserMatches()
    {
        Mock<IContextDocumentParser> mockParser = new();
        mockParser.Setup(p => p.CanParse(It.IsAny<string>())).Returns(false);

        DocumentConnector sut = new([mockParser.Object]);
        RawContextPayload payload = new()
        {
            Documents =
            [
                new ContextDocumentReference
                {
                    Name = "unknown.bin",
                    ContentType = "application/octet-stream",
                    Content = "x"
                }
            ]
        };

        NormalizedContextBatch batch = await sut.NormalizeAsync(payload, CancellationToken.None);

        batch.CanonicalObjects.Should().BeEmpty();
        batch.Warnings.Should().ContainSingle();
        string warning = batch.Warnings[0];
        warning.Should().Contain("unknown.bin", because: "warning must name the skipped document");
        warning.Should().Contain("application/octet-stream", because: "warning must include the content type");
        warning.Should().Contain("ContextDocumentParserPipeline");
        warning.Should().Contain("SupportedContextDocumentContentTypes");
    }

    [Fact]
    public async Task NormalizeAsync_UsesFirstParserInPipelineOrder_WhenMultipleCanParse()
    {
        Mock<IContextDocumentParser> first = new();
        Mock<IContextDocumentParser> second = new();

        first.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);
        second.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);

        first.Setup(p => p.ParseAsync(It.IsAny<ContextDocumentReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CanonicalObject { ObjectType = "Requirement", Name = "from-first", SourceType = "Document" }
            ]);

        second.Setup(p => p.ParseAsync(It.IsAny<ContextDocumentReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CanonicalObject { ObjectType = "Requirement", Name = "from-second", SourceType = "Document" }
            ]);

        DocumentConnector sut = new([first.Object, second.Object]);
        RawContextPayload payload = new()
        {
            Documents =
            [
                new ContextDocumentReference
                {
                    Name = "doc.txt",
                    ContentType = "text/plain",
                    Content = "REQ: x"
                }
            ]
        };

        NormalizedContextBatch batch = await sut.NormalizeAsync(payload, CancellationToken.None);

        batch.CanonicalObjects.Should().ContainSingle().Which.Name.Should().Be("from-first");
        second.Verify(
            p => p.ParseAsync(It.IsAny<ContextDocumentReference>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
