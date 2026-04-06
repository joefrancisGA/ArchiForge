namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Evidence backing a plan: totals plus capped, deterministically ordered reference lists.</summary>
public sealed class LearningPlanningReportPlanEvidenceBlock
{
    public int LinkedSignalCount { get; init; }

    public int LinkedArtifactCount { get; init; }

    public int LinkedArchitectureRunCount { get; init; }

    public IReadOnlyList<LearningPlanningReportSignalRef> Signals { get; init; } = [];

    public IReadOnlyList<LearningPlanningReportArtifactRef> Artifacts { get; init; } = [];

    public IReadOnlyList<string> ArchitectureRunIds { get; init; } = [];
}
