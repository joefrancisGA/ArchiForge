namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// Open vocabulary stored in <c>ProductLearningPilotSignals.SubjectType</c> (no CHECK — evolve without migrations).
/// Prefer stable, lowercase-with-dots or PascalCase tokens agreed by product.
/// </summary>
public static class ProductLearningSubjectTypeValues
{
    public const string RunOutput = "RunOutput";

    public const string ManifestArtifact = "ManifestArtifact";

    public const string ComparisonSummary = "ComparisonSummary";

    public const string AdvisoryRecommendation = "AdvisoryRecommendation";

    public const string Other = "Other";
}
