using ArchLucid.Application.Explanation;
using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Validation;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests;

public sealed class ExplanationServiceComparisonTests
{
    /// <summary>
    ///     LLM comparison JSON with get-only properties does not hydrate; <see cref="DeterministicExplanationService" /> falls
    ///     back to heuristics.
    ///     This test exercises the comparison explanation pipeline and heuristic fallbacks when the model JSON is not bound.
    /// </summary>
    [Fact]
    public async Task ExplainComparisonAsync_returns_heuristic_summary_when_llm_json_does_not_bind()
    {
        IAgentCompletionClient client = new FakeAgentCompletionClient((_, _) => "{}");
        ExplanationService svc = new(
            client,
            new DeterministicExplanationService(NullLogger<DeterministicExplanationService>.Instance),
            Options.Create(new ExplanationServiceOptions()),
            new PassthroughSchemaValidationService(),
            NullLogger<ExplanationService>.Instance);
        ComparisonResult comparison = new()
        {
            BaseRunId = Guid.NewGuid(), TargetRunId = Guid.NewGuid(), SummaryHighlights = ["Highlight A"]
        };

        ComparisonExplanationResult result = await svc.ExplainComparisonAsync(comparison, CancellationToken.None);

        result.HighLevelSummary.Should().NotBeNullOrWhiteSpace();
        result.Narrative.Should().NotBeNullOrWhiteSpace();
        result.MajorChanges.Should().NotBeNull();
    }
}
