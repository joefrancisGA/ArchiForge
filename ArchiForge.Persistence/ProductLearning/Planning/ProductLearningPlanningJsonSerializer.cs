using System.Text.Json;

using ArchiForge.Contracts.ProductLearning.Planning;

namespace ArchiForge.Persistence.ProductLearning.Planning;

internal static class ProductLearningPlanningJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal static string SerializeActionSteps(IReadOnlyList<ProductLearningImprovementPlanActionStep> steps)
    {
        List<ProductLearningImprovementPlanActionStep> ordered = steps
            .OrderBy(static s => s.Ordinal)
            .ToList();

        return JsonSerializer.Serialize(ordered, Options);
    }

    internal static IReadOnlyList<ProductLearningImprovementPlanActionStep> DeserializeActionSteps(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("BoundedActionsJson is missing or empty.");
        }

        List<ProductLearningImprovementPlanActionStep>? list =
            JsonSerializer.Deserialize<List<ProductLearningImprovementPlanActionStep>>(json, Options);

        if (list is null)
        {
            throw new InvalidOperationException("BoundedActionsJson did not deserialize to action steps.");
        }

        if (list.Count == 0)
        {
            throw new InvalidOperationException("BoundedActionsJson did not deserialize to action steps.");
        }

        return list;
    }
}
