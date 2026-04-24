using ArchLucid.ContextIngestion.Connectors;
using ArchLucid.ContextIngestion.Infrastructure;
using ArchLucid.ContextIngestion.Models;

using FluentAssertions;

using Moq;

namespace ArchLucid.ContextIngestion.Tests;

/// <summary>
///     Tests for Infrastructure Declaration Connector.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InfrastructureDeclarationConnectorTests
{
    [Fact]
    public async Task NormalizeAsync_Warns_WhenFormatUnsupported()
    {
        InfrastructureDeclarationConnector sut = new([]);
        RawContextPayload payload = new()
        {
            InfrastructureDeclarations =
            [
                new InfrastructureDeclarationReference
                {
                    Name = "bad.fmt", Format = "hcl", Content = "resource \"x\" \"y\" {}"
                }
            ]
        };

        NormalizedContextBatch batch = await sut.NormalizeAsync(payload, CancellationToken.None);

        batch.CanonicalObjects.Should().BeEmpty();
        batch.Warnings.Should().ContainSingle().Which.Should().Contain("bad.fmt").And.Contain("hcl");
    }

    [Fact]
    public async Task NormalizeAsync_DelegatesToParser()
    {
        Mock<IInfrastructureDeclarationParser> parser = new();
        parser.Setup(p => p.CanParse("json")).Returns(true);
        parser.Setup(p => p.ParseAsync(It.IsAny<InfrastructureDeclarationReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CanonicalObject>
            {
                new()
                {
                    ObjectType = "TopologyResource",
                    Name = "a",
                    SourceType = "InfrastructureDeclaration",
                    SourceId = "d",
                    Properties = new Dictionary<string, string> { ["resourceType"] = "vnet" }
                }
            });

        InfrastructureDeclarationConnector sut = new([parser.Object]);
        RawContextPayload payload = new()
        {
            InfrastructureDeclarations =
            [
                new InfrastructureDeclarationReference { Name = "x", Format = "json", Content = "{}" }
            ]
        };

        NormalizedContextBatch batch = await sut.NormalizeAsync(payload, CancellationToken.None);

        batch.CanonicalObjects.Should().HaveCount(1);
        parser.Verify(p => p.ParseAsync(It.IsAny<InfrastructureDeclarationReference>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
