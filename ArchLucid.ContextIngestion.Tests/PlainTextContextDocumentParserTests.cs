using ArchLucid.ContextIngestion.Models;
using ArchLucid.ContextIngestion.Parsing;

using FluentAssertions;

namespace ArchLucid.ContextIngestion.Tests;

/// <summary>
///     Tests for Plain Text Context Document Parser.
/// </summary>
[Trait("Suite", "Core")]
public sealed class PlainTextContextDocumentParserTests
{
    private readonly PlainTextContextDocumentParser _sut = new();

    [Fact]
    public async Task ParseAsync_ExtractsPrefixedLines()
    {
        ContextDocumentReference doc = new()
        {
            Name = "spec.md",
            ContentType = "text/markdown",
            Content = """
                      REQ: System must be HA
                      POL: SOC2 alignment
                      TOP: subnet-ingress
                      SEC: encrypt at rest
                      """
        };

        IReadOnlyList<CanonicalObject> result = await _sut.ParseAsync(doc, CancellationToken.None);

        result.Should().HaveCount(4);
        result.Select(o => o.ObjectType).Should().Equal(
            "Requirement",
            "PolicyControl",
            "TopologyResource",
            "SecurityBaseline");
        result[0].Properties["text"].Should().Be("System must be HA");
        result[0].SourceId.Should().Be(doc.DocumentId);
    }
}
