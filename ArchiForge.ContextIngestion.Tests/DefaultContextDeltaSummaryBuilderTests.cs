using ArchiForge.ContextIngestion.Models;
using ArchiForge.ContextIngestion.Summaries;

using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

public sealed class DefaultContextDeltaSummaryBuilderTests
{
    private readonly DefaultContextDeltaSummaryBuilder _sut = new();

    [Fact]
    public void BuildSegment_IncludesBaseline_WhenFirstConnector()
    {
        ContextSnapshot previous = new ContextSnapshot
        {
            CanonicalObjects =
            [
                new CanonicalObject
                {
                    ObjectType = "Requirement",
                    Name = "old",
                    SourceType = "x",
                    SourceId = "y",
                    Properties = new Dictionary<string, string> { ["text"] = "old" }
                }
            ]
        };

        NormalizedContextBatch batch = new NormalizedContextBatch
        {
            CanonicalObjects =
            [
                new CanonicalObject
                {
                    ObjectType = "Requirement",
                    Name = "n",
                    SourceType = "InlineRequirement",
                    SourceId = "inline",
                    Properties = new Dictionary<string, string> { ["text"] = "a" }
                }
            ]
        };

        string line = _sut.BuildSegment("inline-requirements", "Initial inline", batch, previous, isFirstConnector: true);

        line.Should().Contain("Initial inline");
        line.Should().Contain("prior snapshot had 1 canonical object");
        line.Should().Contain("Requirement×1");
    }

    [Fact]
    public void BuildSegment_OmitsBaselineClause_WhenNotFirstConnector()
    {
        NormalizedContextBatch batch = new NormalizedContextBatch
        {
            CanonicalObjects = []
        };

        string line = _sut.BuildSegment("policy-reference", "Updated policy", batch, null, isFirstConnector: false);

        line.Should().NotContain("baseline");
        line.Should().Contain("0 produced");
    }
}
