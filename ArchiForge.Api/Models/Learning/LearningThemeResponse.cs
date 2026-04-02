namespace ArchiForge.Api.Models.Learning;

/// <summary>59R improvement theme row for operator UI (maps from persisted theme record).</summary>
public sealed class LearningThemeResponse
{
    public Guid ThemeId { get; init; }

    public string ThemeKey { get; init; } = string.Empty;

    public string? SourceAggregateKey { get; init; }

    public string? PatternKey { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string AffectedArtifactTypeOrWorkflowArea { get; init; } = string.Empty;

    public string SeverityBand { get; init; } = string.Empty;

    /// <summary>Count of pilot signals rolled into this theme (evidence volume).</summary>
    public int EvidenceSignalCount { get; init; }

    public int DistinctRunCount { get; init; }

    public double? AverageTrustScore { get; init; }

    public string DerivationRuleVersion { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedUtc { get; init; }

    public string? CreatedByUserId { get; init; }
}
