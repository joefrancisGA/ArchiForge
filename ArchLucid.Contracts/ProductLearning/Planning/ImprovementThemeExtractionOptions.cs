namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Thresholds for deterministic theme extraction from 58R aggregates and raw signals.</summary>
public sealed class ImprovementThemeExtractionOptions
{
    /// <summary>Minimum total signals on a rollup for it to become a run-feedback theme.</summary>
    public int MinSignalsPerAggregateTheme { get; init; } = 2;

    /// <summary>Minimum rejected + needs-follow-up count to treat outcomes as a repeated negative pattern.</summary>
    public int MinRejectedOrFollowUpForOutcomePattern { get; init; } = 2;

    /// <summary>Minimum revised count to treat outcomes as a repeated revision pattern.</summary>
    public int MinRevisedForRevisionPattern { get; init; } = 2;

    /// <summary>Minimum total signals on an artifact trend row.</summary>
    public int MinSignalsPerArtifactTrend { get; init; } = 2;

    /// <summary>Minimum negative outcomes (reject + revise + follow-up) on a trend row.</summary>
    public int MinNegativeOutcomesOnArtifactTrend { get; init; } = 2;

    /// <summary>Minimum occurrences for a repeated comment prefix theme (matches 58R comment rollups).</summary>
    public int MinCommentOccurrences { get; init; } = 2;

    /// <summary>Minimum pilot rows sharing the same normalized tag/annotation token from <c>DetailJson</c>.</summary>
    public int MinTagOccurrences { get; init; } = 3;

    /// <summary>Cap on example evidence rows attached to each theme.</summary>
    public int MaxExampleEvidencePerTheme { get; init; } = 5;

    /// <summary>Upper bound on themes returned (after merge and sort).</summary>
    public int MaxThemes { get; init; } = 200;
}
