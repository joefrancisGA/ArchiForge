using ArchLucid.Application.Pilots;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
public sealed class WhyArchLucidPackBuilderTests
{
    [Fact]
    public void BuildMarkdown_includes_competitive_landscape_anchor_and_demo_banner()
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

        md.Should().Contain("docs/go-to-market/COMPETITIVE_LANDSCAPE.md");
        md.Should().Contain("§2.1");
        md.Should().Contain("demo tenant — replace before publishing");
        md.Should().Contain("`run`");
    }
}
