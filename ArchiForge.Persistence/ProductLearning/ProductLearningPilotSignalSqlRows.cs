using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Persistence.ProductLearning;

/// <summary>Dapper row shapes for product-learning aggregation queries (explicit, auditable projections).</summary>
[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class FeedbackAggregateSqlRow
{
    public string AggregateKey { get; init; } = string.Empty;

    public string? PatternKeyRaw { get; init; }

    public string SubjectTypeOrWorkflowArea { get; init; } = string.Empty;

    public int DistinctRunCount { get; init; }

    public int TotalSignalCount { get; init; }

    public int TrustedCount { get; init; }

    public int RejectedCount { get; init; }

    public int RevisedCount { get; init; }

    public int NeedsFollowUpCount { get; init; }

    public string? DominantThemeHint { get; init; }

    public DateTime FirstSignalRecordedUtc { get; init; }

    public DateTime LastSignalRecordedUtc { get; init; }
}

[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class ArtifactOutcomeTrendSqlRow
{
    public string TrendKey { get; init; } = string.Empty;

    public string ArtifactTypeOrHint { get; init; } = string.Empty;

    public int AcceptedOrTrustedCount { get; init; }

    public int RevisionCount { get; init; }

    public int RejectionCount { get; init; }

    public int NeedsFollowUpCount { get; init; }

    public int DistinctRunCount { get; init; }

    public string? RepeatedThemeIndicator { get; init; }

    public DateTime FirstSeenUtc { get; init; }

    public DateTime LastSeenUtc { get; init; }
}

[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class RepeatedCommentThemeSqlRow
{
    public string ThemeKey { get; init; } = string.Empty;

    public long OccurrenceCount { get; init; }

    public DateTime FirstSeenUtc { get; init; }

    public DateTime LastSeenUtc { get; init; }

    public string SampleCommentShort { get; init; } = string.Empty;
}
