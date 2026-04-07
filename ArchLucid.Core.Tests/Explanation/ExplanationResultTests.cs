using ArchLucid.Core.Explanation;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Explanation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ExplanationResultTests
{
    [Fact]
    public void ExplanationResult_Defaults_AreEmptyCollectionsAndStrings()
    {
        ExplanationResult sut = new();

        sut.RawText.Should().BeEmpty();
        sut.Structured.Should().BeNull();
        sut.Summary.Should().BeEmpty();
        sut.DetailedNarrative.Should().BeEmpty();
        sut.KeyDrivers.Should().NotBeNull().And.BeEmpty();
        sut.RiskImplications.Should().NotBeNull().And.BeEmpty();
        sut.CostImplications.Should().NotBeNull().And.BeEmpty();
        sut.ComplianceImplications.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ExplanationResult_SetsAndReads_AllProperties()
    {
        ExplanationResult sut = new();

        sut.RawText = "raw";
        sut.Structured = new StructuredExplanation { Reasoning = "r" };
        sut.Summary = "summary";
        sut.DetailedNarrative = "details";
        sut.KeyDrivers = ["k1"];
        sut.RiskImplications = ["r1"];
        sut.CostImplications = ["c1"];
        sut.ComplianceImplications = ["m1"];

        sut.RawText.Should().Be("raw");
        sut.Structured!.Reasoning.Should().Be("r");
        sut.Summary.Should().Be("summary");
        sut.DetailedNarrative.Should().Be("details");
        sut.KeyDrivers.Should().Equal("k1");
        sut.RiskImplications.Should().Equal("r1");
        sut.CostImplications.Should().Equal("c1");
        sut.ComplianceImplications.Should().Equal("m1");
    }

    [Fact]
    public void ComparisonExplanationResult_Defaults_AreEmptyCollectionsAndStrings()
    {
        ComparisonExplanationResult sut = new();

        sut.HighLevelSummary.Should().BeEmpty();
        sut.Narrative.Should().BeEmpty();
        sut.MajorChanges.Should().NotBeNull().And.BeEmpty();
        sut.KeyTradeoffs.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ComparisonExplanationResult_SetsAndReads_AllProperties()
    {
        ComparisonExplanationResult sut = new();

        sut.HighLevelSummary = "high";
        sut.Narrative = "long";
        sut.MajorChanges = ["a"];
        sut.KeyTradeoffs = ["b"];

        sut.HighLevelSummary.Should().Be("high");
        sut.Narrative.Should().Be("long");
        sut.MajorChanges.Should().Equal("a");
        sut.KeyTradeoffs.Should().Equal("b");
    }
}
