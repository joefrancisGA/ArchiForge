using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.ProductLearning;

public sealed class ProductLearningPlanningJsonSerializerTests
{
    [Fact]
    public void Serialize_then_deserialize_round_trips_ordered_by_ordinal()
    {
        IReadOnlyList<ProductLearningImprovementPlanActionStep> steps =
        [
            new() { Ordinal = 2, ActionType = "B", Description = "second" },
            new() { Ordinal = 1, ActionType = "A", Description = "first" }
        ];

        string json = ProductLearningPlanningJsonSerializer.SerializeActionSteps(steps);
        IReadOnlyList<ProductLearningImprovementPlanActionStep> back =
            ProductLearningPlanningJsonSerializer.DeserializeActionSteps(json);

        back.Should().HaveCount(2);
        back[0].Ordinal.Should().Be(1);
        back[1].Ordinal.Should().Be(2);
    }

    [Fact]
    public void Deserialize_throws_when_empty()
    {
        Action act = () => ProductLearningPlanningJsonSerializer.DeserializeActionSteps("  ");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deserialize_throws_when_json_array_empty()
    {
        Action act = () => ProductLearningPlanningJsonSerializer.DeserializeActionSteps("[]");

        act.Should().Throw<InvalidOperationException>();
    }
}
