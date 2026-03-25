using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Models;

using FluentAssertions;

using Moq;

namespace ArchiForge.ContextIngestion.Tests;

public sealed class DocumentConnectorTests
{
    [Fact]
    public async Task NormalizeAsync_AddsWarning_WhenNoParserMatches()
    {
        Mock<IContextDocumentParser> mockParser = new Mock<IContextDocumentParser>();
        mockParser.Setup(p => p.CanParse(It.IsAny<string>())).Returns(false);

        DocumentConnector sut = new DocumentConnector([mockParser.Object]);
        RawContextPayload payload = new RawContextPayload
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
        batch.Warnings.Should().ContainSingle()
            .Which.Should().Contain("unknown.bin")
            .And.Contain("application/octet-stream");
    }
}
