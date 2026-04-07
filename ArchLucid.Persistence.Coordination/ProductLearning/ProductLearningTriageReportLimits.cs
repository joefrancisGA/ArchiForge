namespace ArchLucid.Persistence.Coordination.ProductLearning;

/// <summary>Caps for rows included in triage reports (noise control).</summary>
public sealed class ProductLearningTriageReportLimits
{
    public int MaxArtifactRows { get; init; } = 10;
    public int MaxImprovements { get; init; } = 10;
    public int MaxTriagePreview { get; init; } = 15;
    public int MaxProblemAreaLines { get; init; } = 8;
    public int MaxSummaryChars { get; init; } = 240;
}
