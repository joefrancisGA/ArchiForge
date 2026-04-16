using ArchLucid.AgentRuntime.Safety;
using ArchLucid.Core.Safety;

using Azure.AI.ContentSafety;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.Safety;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AzureContentSafetyGuardMapResultTests
{
    [Fact]
    public void MapResult_allows_when_no_categories()
    {
        AnalyzeTextResult result = ContentSafetyModelFactory.AnalyzeTextResult([], []);

        ContentSafetyResult mapped = AzureContentSafetyGuard.MapResult(result, blockSeverityThreshold: 4);

        mapped.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void MapResult_blocks_when_severity_meets_threshold()
    {
        AnalyzeTextResult result = ContentSafetyModelFactory.AnalyzeTextResult(
            [],
            [ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 6)]);

        ContentSafetyResult mapped = AzureContentSafetyGuard.MapResult(result, blockSeverityThreshold: 4);

        mapped.IsAllowed.Should().BeFalse();
        mapped.Category.Should().Be(TextCategory.Hate.ToString());
        mapped.Severity.Should().Be(6);
    }

    [Fact]
    public void MapResult_allows_when_severity_below_threshold()
    {
        AnalyzeTextResult result = ContentSafetyModelFactory.AnalyzeTextResult(
            [],
            [ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Violence, 2)]);

        ContentSafetyResult mapped = AzureContentSafetyGuard.MapResult(result, blockSeverityThreshold: 4);

        mapped.IsAllowed.Should().BeTrue();
    }
}
