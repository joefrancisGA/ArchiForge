namespace ArchiForge.Contracts.Evolution;

/// <summary>Read-side pipeline sections for shadow execution (evidence, traces, compares, determinism are always off).</summary>
public sealed class ShadowExecutionPipelineOptions
{
    public bool IncludeManifest { get; init; } = true;

    public bool IncludeSummary { get; init; } = true;

    public bool IncludeDiagram { get; init; }
}
