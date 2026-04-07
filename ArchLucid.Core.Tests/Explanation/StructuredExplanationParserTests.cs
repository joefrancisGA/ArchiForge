using ArchLucid.Core.Explanation;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Explanation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class StructuredExplanationParserTests
{
    [Fact]
    public void TryNormalizeStructuredJson_happy_path_all_fields()
    {
        const string json =
            """
            {"schemaVersion":1,"reasoning":"Main text","evidenceRefs":["a"],"confidence":0.5,"alternativesConsidered":["b"],"caveats":["c"]}
            """;

        bool ok = StructuredExplanationParser.TryNormalizeStructuredJson(json, out StructuredExplanation? s);

        ok.Should().BeTrue();
        s.Should().NotBeNull();
        s!.Reasoning.Should().Be("Main text");
        s.EvidenceRefs.Should().Equal("a");
        s.Confidence.Should().Be(0.5m);
        s.AlternativesConsidered.Should().Equal("b");
        s.Caveats.Should().Equal("c");
    }

    [Fact]
    public void TryNormalizeStructuredJson_minimal_reasoning_only()
    {
        const string json = """{"reasoning":"Only this"}""";

        bool ok = StructuredExplanationParser.TryNormalizeStructuredJson(json, out StructuredExplanation? s);

        ok.Should().BeTrue();
        s!.SchemaVersion.Should().Be(1);
        s.Reasoning.Should().Be("Only this");
        s.EvidenceRefs.Should().BeEmpty();
    }

    [Fact]
    public void Parse_plain_text_wraps_as_reasoning()
    {
        StructuredExplanation s = StructuredExplanationParser.Parse("This is a free-text explanation");

        s.Reasoning.Should().Be("This is a free-text explanation");
        s.SchemaVersion.Should().Be(1);
        s.EvidenceRefs.Should().BeEmpty();
        s.Confidence.Should().BeNull();
    }

    [Fact]
    public void Parse_malformed_json_falls_back_to_raw_string()
    {
        const string raw = "{broken";

        StructuredExplanation s = StructuredExplanationParser.Parse(raw);

        s.Reasoning.Should().Be(raw);
    }

    [Fact]
    public void TryNormalizeStructuredJson_empty_reasoning_falls_back_to_Parse()
    {
        const string json = """{"schemaVersion":1,"reasoning":""}""";

        bool ok = StructuredExplanationParser.TryNormalizeStructuredJson(json, out _);

        ok.Should().BeFalse();

        StructuredExplanation s = StructuredExplanationParser.Parse(json);

        s.Reasoning.Should().Be(json);
    }

    [Fact]
    public void Parse_null_or_whitespace_yields_empty_reasoning()
    {
        StructuredExplanationParser.Parse(null).Reasoning.Should().BeEmpty();
        StructuredExplanationParser.Parse("   ").Reasoning.Should().BeEmpty();
    }

    [Fact]
    public void ClampConfidence_clamps_to_unit_interval()
    {
        StructuredExplanationParser.ClampConfidence(null).Should().BeNull();
        StructuredExplanationParser.ClampConfidence(-0.1m).Should().Be(0m);
        StructuredExplanationParser.ClampConfidence(1.1m).Should().Be(1m);
        StructuredExplanationParser.ClampConfidence(0.25m).Should().Be(0.25m);
    }
}
