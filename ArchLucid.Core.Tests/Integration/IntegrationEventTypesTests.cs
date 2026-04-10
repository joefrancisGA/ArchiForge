using ArchLucid.Core.Integration;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Integration;

public sealed class IntegrationEventTypesTests
{
    [Fact]
    public void MapToCanonical_trims_whitespace()
    {
        IntegrationEventTypes.MapToCanonical($"  {IntegrationEventTypes.AuthorityRunCompletedV1}  ")
            .Should()
            .Be(IntegrationEventTypes.AuthorityRunCompletedV1);
    }

    [Fact]
    public void AreEquivalent_matches_trimmed_identical_types()
    {
        IntegrationEventTypes.AreEquivalent(
                $" {IntegrationEventTypes.AlertFiredV1}",
                $"{IntegrationEventTypes.AlertFiredV1} ")
            .Should()
            .BeTrue();
    }

    [Fact]
    public void AreEquivalent_false_for_unrelated_types()
    {
        IntegrationEventTypes.AreEquivalent(
                IntegrationEventTypes.AlertFiredV1,
                IntegrationEventTypes.AlertResolvedV1)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void AreEquivalent_false_when_either_side_empty()
    {
        IntegrationEventTypes.AreEquivalent("", IntegrationEventTypes.AlertFiredV1).Should().BeFalse();
        IntegrationEventTypes.AreEquivalent(IntegrationEventTypes.AlertFiredV1, "   ").Should().BeFalse();
    }
}
