using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.AgentRuntime.Tests.Explanation;

[Trait("Category", "Unit")]
public sealed class DeterministicExplanationServiceTests
{
    [Fact]
    public void ExtractMajorChanges_maps_decision_deltas()
    {
        DeterministicExplanationService sut = new(NullLogger<DeterministicExplanationService>.Instance);
        ComparisonResult comparison = new()
        {
            DecisionChanges =
            [
                new DecisionDelta { ChangeType = "Added", DecisionKey = "storage", TargetValue = "blob" }
            ]
        };

        List<string> lines = sut.ExtractMajorChanges(comparison);

        lines.Should().Contain(s =>
            s.Contains("storage", StringComparison.OrdinalIgnoreCase)
            && s.Contains("added", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildComparisonExplanation_uses_heuristic_when_json_empty_object()
    {
        DeterministicExplanationService sut = new(NullLogger<DeterministicExplanationService>.Instance);
        ComparisonResult comparison = new()
        {
            DecisionChanges =
            [
                new DecisionDelta { ChangeType = "Modified", DecisionKey = "k", BaseValue = "a", TargetValue = "b" }
            ]
        };
        List<string> major = sut.ExtractMajorChanges(comparison);

        ComparisonExplanationResult r = sut.BuildComparisonExplanation(comparison, major, "{}");

        r.HighLevelSummary.Should().NotBeNullOrWhiteSpace();
        r.Narrative.Should().NotBeNullOrWhiteSpace();
    }
}
