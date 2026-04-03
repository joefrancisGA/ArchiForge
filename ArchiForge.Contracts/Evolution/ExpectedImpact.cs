namespace ArchiForge.Contracts.Evolution;

/// <summary>Human-readable expected impact of applying the candidate (derived from plan/theme signals; not measured until simulation runs).</summary>
public sealed class ExpectedImpact
{
    public string Summary { get; init; } = string.Empty;

    public string? Rationale { get; init; }
}
