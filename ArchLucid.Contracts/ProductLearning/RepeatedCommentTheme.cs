namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// Deterministic rollup of identical leading text in <see cref="ProductLearningPilotSignalRecord.CommentShort"/>
/// (trimmed, first 200 chars). Used for spotting repeated pilot wording without NLP.
/// </summary>
public sealed class RepeatedCommentTheme
{
    /// <summary>Normalized key used for grouping (prefix of comment text).</summary>
    public string ThemeKey { get; init; } = string.Empty;
    public int OccurrenceCount { get; init; }
    public DateTime FirstSeenUtc { get; init; }
    public DateTime LastSeenUtc { get; init; }

    /// <summary>Lexicographically smallest non-empty comment in the bucket (deterministic sample).</summary>
    public string SampleCommentShort { get; init; } = string.Empty;
}
