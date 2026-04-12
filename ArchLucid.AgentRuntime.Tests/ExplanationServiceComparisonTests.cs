using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests;

public sealed class ExplanationServiceComparisonTests
{
    /// <summary>
    /// <see cref="ExplanationService.LlmComparisonJson"/> uses get-only properties; System.Text.Json does not hydrate them from the model response.
    /// This test exercises the comparison explanation pipeline and heuristic fallbacks when the model JSON is not bound.
    /// </summary>
    [Fact]
    public async Task ExplainComparisonAsync_returns_heuristic_summary_when_llm_json_does_not_bind()
    {
        IAgentCompletionClient client = new FakeAgentCompletionClient((_, _) => "{}");
        ExplanationService svc = new(
            client,
            Options.Create(new ExplanationServiceOptions()),
            NullLogger<ExplanationService>.Instance);
        ComparisonResult comparison = new()
        {
            BaseRunId = Guid.NewGuid(),
            TargetRunId = Guid.NewGuid(),
            SummaryHighlights = ["Highlight A"],
        };

        ComparisonExplanationResult result = await svc.ExplainComparisonAsync(comparison, CancellationToken.None);

        result.HighLevelSummary.Should().NotBeNullOrWhiteSpace();
        result.Narrative.Should().NotBeNullOrWhiteSpace();
        result.MajorChanges.Should().NotBeNull();
    }
}
