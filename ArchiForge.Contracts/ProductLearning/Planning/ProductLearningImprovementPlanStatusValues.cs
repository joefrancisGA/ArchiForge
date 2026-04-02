namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Lifecycle for a bounded improvement plan (review gate; no autonomous execution).</summary>
public static class ProductLearningImprovementPlanStatusValues
{
    public const string Proposed = "Proposed";

    public const string UnderReview = "UnderReview";

    public const string Approved = "Approved";

    public const string Rejected = "Rejected";

    public const string Completed = "Completed";
}
