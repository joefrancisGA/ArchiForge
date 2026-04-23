using ArchLucid.Application.Pilots;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
public sealed class WhyArchLucidPackBuilderTests
{
    [Fact]
    public void BuildMarkdown_includes_benchmarked_differentiation_table_and_demo_banner()
    {
        WhyArchLucidPackSourceDto source = new(
            RunId: "run",
            ProjectId: "proj",
            ManifestSectionMarkdown: "| a | b |\n|---|---|\n| x | y |",
            AuthorityChainSectionMarkdown: "| id | v |\n|----|---|\n| a | b |",
            ArtifactsSectionMarkdown: "_none_",
            PipelineTimelineSectionMarkdown: "_none_",
            RunExplanationSectionMarkdown: "**Summary**\n\nhello",
            CitationsSectionMarkdown: "_none_",
            ComparisonDeltaSampleMarkdown: "- theme");

        string md = WhyArchLucidPackBuilder.BuildMarkdown(source);

        md.Should().Contain("Five capability claims, every claim cited to a file in this repository or to an external public source.");
        md.Should().Contain("| Claim | ArchLucid evidence | Competitor baseline | Citation | Narrative (≤4 sentences) |");
        md.Should().Contain("GET /v1/authority/runs/{runId}/provenance");
        md.Should().Contain("demo tenant — replace before publishing");
        md.Should().Contain("`run`");
    }
}
