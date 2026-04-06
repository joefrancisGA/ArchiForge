namespace ArchiForge.Api.Models.Learning;

/// <summary>Explicit link counts for a plan (signals, artifacts, architecture runs).</summary>
public sealed class LearningPlanEvidenceCountsResponse
{
    public int LinkedSignalCount { get; init; }

    public int LinkedArtifactCount { get; init; }

    public int LinkedArchitectureRunCount { get; init; }
}
