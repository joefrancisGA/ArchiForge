namespace ArchiForge.Api.Models;

public sealed class ReplayComparisonRequest
{
    public string Format { get; set; } = "markdown";

    /// <summary>Export profile for end-to-end comparison: default, short, detailed, executive.</summary>
    public string? Profile { get; set; }
}

