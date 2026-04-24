using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.ProductLearning;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

namespace ArchLucid.Persistence.Tests.ProductLearning.Planning;

/// <summary>59R theme extraction from 58R aggregates and scoped pilot signals.</summary>
[Trait("ChangeSet", "59R")]
public sealed class ImprovementThemeExtractionServiceTests
{
    /// <summary>
    ///     Pilot signals matching the "small" rollup <see cref="FeedbackAggregate.TotalSignalCount" /> in theme-ranking
    ///     tests.
    /// </summary>
    private const int SmallAggregatePilotSignalCount = 2;

    /// <summary>
    ///     Pilot signals matching the "big" rollup <see cref="FeedbackAggregate.TotalSignalCount" /> in theme-ranking
    ///     tests.
    /// </summary>
    private const int BigAggregatePilotSignalCount = 8;

    private static ProductLearningScope Scope()
    {
        return new ProductLearningScope
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };
    }

    private static ProductLearningPilotSignalRecord Signal(
        Guid signalId,
        string disposition,
        string? patternKey,
        string subject,
        string? artifactHint,
        string? comment,
        string? detailJson,
        DateTime utc)
    {
        ProductLearningScope s = Scope();

        return new ProductLearningPilotSignalRecord
        {
            SignalId = signalId,
            TenantId = s.TenantId,
            WorkspaceId = s.WorkspaceId,
            ProjectId = s.ProjectId,
            SubjectType = subject,
            Disposition = disposition,
            PatternKey = patternKey,
            ArtifactHint = artifactHint,
            CommentShort = comment,
            DetailJson = detailJson,
            ArchitectureRunId = "run-a",
            RecordedUtc = utc
        };
    }

    [Fact]
    public async Task Extract_empty_inputs_yields_empty()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope, FeedbackRollups = [], ArtifactTrends = [], RepeatedCommentThemes = []
        };

        IReadOnlyList<ImprovementThemeWithEvidence> themes = await svc.ExtractThemesAsync(
            snapshot,
            [],
            null,
            new ImprovementThemeExtractionOptions(),
            CancellationToken.None);

        Assert.Empty(themes);
    }

    [Fact]
    public async Task Rollup_theme_emits_when_rejects_repeat_and_evidence_matches_signals()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();
        DateTime utc = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        string aggregateKey = "pat-x";

        FeedbackAggregate aggregate = new()
        {
            AggregateKey = aggregateKey,
            PatternKey = "pat-x",
            SubjectTypeOrWorkflowArea = ProductLearningSubjectTypeValues.RunOutput,
            DistinctRunCount = 1,
            TotalSignalCount = 3,
            TrustedCount = 0,
            RejectedCount = 2,
            RevisedCount = 0,
            NeedsFollowUpCount = 1,
            FirstSignalRecordedUtc = utc,
            LastSignalRecordedUtc = utc.AddHours(1)
        };

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope, FeedbackRollups = [aggregate], ArtifactTrends = [], RepeatedCommentThemes = []
        };

        List<ProductLearningPilotSignalRecord> signals =
        [
            Signal(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ProductLearningDispositionValues.Rejected,
                "pat-x",
                ProductLearningSubjectTypeValues.RunOutput,
                null,
                null,
                null,
                utc),
            Signal(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ProductLearningDispositionValues.Rejected,
                "pat-x",
                ProductLearningSubjectTypeValues.RunOutput,
                null,
                null,
                null,
                utc.AddMinutes(1)),
            Signal(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                ProductLearningDispositionValues.NeedsFollowUp,
                "pat-x",
                ProductLearningSubjectTypeValues.RunOutput,
                null,
                null,
                null,
                utc.AddMinutes(2))
        ];

        IReadOnlyList<ImprovementThemeWithEvidence> themes = await svc.ExtractThemesAsync(
            snapshot,
            signals,
            null,
            new ImprovementThemeExtractionOptions { MinSignalsPerAggregateTheme = 2 },
            CancellationToken.None);

        ImprovementThemeWithEvidence rollup = Assert.Single(
            themes,
            t => t.CanonicalKey.StartsWith("rollup:", StringComparison.Ordinal));

        Assert.Equal("rollup:" + aggregateKey, rollup.CanonicalKey);
        Assert.Equal(3, rollup.Theme.EvidenceCount);
        Assert.Equal(3, rollup.ExampleEvidence.Count);
        Assert.Contains("aggregate key", rollup.GroupingExplanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tag_theme_requires_distinct_signals_at_threshold()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();
        DateTime utc = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope, FeedbackRollups = [], ArtifactTrends = [], RepeatedCommentThemes = []
        };

        string detail = """{"tags":["alpha"]}""";

        List<ProductLearningPilotSignalRecord> signals =
        [
            Signal(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                ProductLearningDispositionValues.Trusted,
                null,
                ProductLearningSubjectTypeValues.RunOutput,
                "diagram",
                null,
                detail,
                utc),
            Signal(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                ProductLearningDispositionValues.Trusted,
                null,
                ProductLearningSubjectTypeValues.RunOutput,
                "diagram",
                null,
                detail,
                utc.AddMinutes(1))
        ];

        IReadOnlyList<ImprovementThemeWithEvidence> below = await svc.ExtractThemesAsync(
            snapshot,
            signals,
            null,
            new ImprovementThemeExtractionOptions { MinTagOccurrences = 3 },
            CancellationToken.None);

        Assert.DoesNotContain(below, t => t.CanonicalKey.StartsWith("tag:", StringComparison.Ordinal));

        List<ProductLearningPilotSignalRecord> three =
        [
            ..signals, Signal(
                Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                ProductLearningDispositionValues.Trusted,
                null,
                ProductLearningSubjectTypeValues.RunOutput,
                "diagram",
                null,
                detail,
                utc.AddMinutes(2))
        ];

        IReadOnlyList<ImprovementThemeWithEvidence> ok = await svc.ExtractThemesAsync(
            snapshot,
            three,
            null,
            new ImprovementThemeExtractionOptions { MinTagOccurrences = 3 },
            CancellationToken.None);

        ImprovementThemeWithEvidence tagTheme = Assert.Single(ok);

        Assert.StartsWith("tag:alpha", tagTheme.CanonicalKey, StringComparison.Ordinal);
        Assert.Equal(3, tagTheme.Theme.EvidenceCount);
    }

    [Fact]
    public async Task Mismatched_signal_scope_throws()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope, FeedbackRollups = [], ArtifactTrends = [], RepeatedCommentThemes = []
        };

        ProductLearningPilotSignalRecord badScope = new()
        {
            SignalId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            SubjectType = ProductLearningSubjectTypeValues.RunOutput,
            Disposition = ProductLearningDispositionValues.Trusted,
            RecordedUtc = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<ArgumentException>(() => svc.ExtractThemesAsync(
            snapshot,
            [badScope],
            null,
            new ImprovementThemeExtractionOptions(),
            CancellationToken.None));
    }

    [Fact]
    public async Task Theme_ids_are_stable_for_same_scope_and_key()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();
        DateTime utc = new(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);

        FeedbackAggregate aggregate = new()
        {
            AggregateKey = "stable-key",
            PatternKey = "stable-key",
            SubjectTypeOrWorkflowArea = ProductLearningSubjectTypeValues.RunOutput,
            DistinctRunCount = 1,
            TotalSignalCount = 2,
            TrustedCount = 0,
            RejectedCount = 2,
            RevisedCount = 0,
            NeedsFollowUpCount = 0,
            FirstSignalRecordedUtc = utc,
            LastSignalRecordedUtc = utc
        };

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope, FeedbackRollups = [aggregate], ArtifactTrends = [], RepeatedCommentThemes = []
        };

        List<ProductLearningPilotSignalRecord> signals =
        [
            Signal(
                Guid.NewGuid(),
                ProductLearningDispositionValues.Rejected,
                "stable-key",
                ProductLearningSubjectTypeValues.RunOutput,
                null,
                null,
                null,
                utc),
            Signal(
                Guid.NewGuid(),
                ProductLearningDispositionValues.Rejected,
                "stable-key",
                ProductLearningSubjectTypeValues.RunOutput,
                null,
                null,
                null,
                utc)
        ];

        ImprovementThemeExtractionOptions options = new() { MinSignalsPerAggregateTheme = 2 };

        IReadOnlyList<ImprovementThemeWithEvidence> a = await svc.ExtractThemesAsync(
            snapshot,
            signals,
            null,
            options,
            CancellationToken.None);

        IReadOnlyList<ImprovementThemeWithEvidence> b = await svc.ExtractThemesAsync(
            snapshot,
            signals,
            null,
            options,
            CancellationToken.None);

        Assert.Equal(a[0].Theme.ThemeId, b[0].Theme.ThemeId);
    }

    [Fact]
    public async Task Trend_theme_emits_when_totals_and_negative_outcomes_meet_thresholds()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();
        DateTime utc = new(2026, 4, 4, 0, 0, 0, DateTimeKind.Utc);

        string trendKey = ProductLearningSignalAggregations.BuildTrendKey(
            ProductLearningSubjectTypeValues.ManifestArtifact,
            "export.pdf");

        ArtifactOutcomeTrend trend = new()
        {
            TrendKey = trendKey,
            ArtifactTypeOrHint = "export.pdf",
            AcceptedOrTrustedCount = 1,
            RejectionCount = 2,
            RevisionCount = 0,
            NeedsFollowUpCount = 1,
            DistinctRunCount = 2,
            FirstSeenUtc = utc,
            LastSeenUtc = utc.AddHours(2)
        };

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope, FeedbackRollups = [], ArtifactTrends = [trend], RepeatedCommentThemes = []
        };

        List<ProductLearningPilotSignalRecord> signals =
        [
            Signal(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ProductLearningDispositionValues.Rejected,
                null,
                ProductLearningSubjectTypeValues.ManifestArtifact,
                "export.pdf",
                null,
                null,
                utc),
            Signal(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ProductLearningDispositionValues.NeedsFollowUp,
                null,
                ProductLearningSubjectTypeValues.ManifestArtifact,
                "export.pdf",
                null,
                null,
                utc.AddMinutes(1)),
            Signal(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ProductLearningDispositionValues.Trusted,
                null,
                ProductLearningSubjectTypeValues.ManifestArtifact,
                "export.pdf",
                null,
                null,
                utc.AddMinutes(2))
        ];

        IReadOnlyList<ImprovementThemeWithEvidence> themes = await svc.ExtractThemesAsync(
            snapshot,
            signals,
            null,
            new ImprovementThemeExtractionOptions
            {
                MinSignalsPerArtifactTrend = 3, MinNegativeOutcomesOnArtifactTrend = 2
            },
            CancellationToken.None);

        ImprovementThemeWithEvidence t = Assert.Single(
            themes,
            x => x.CanonicalKey.StartsWith("trend:", StringComparison.Ordinal));

        Assert.Equal("trend:" + trendKey, t.CanonicalKey);
        Assert.Equal(4, t.Theme.EvidenceCount);
        Assert.Equal(3, t.ExampleEvidence.Count);
    }

    [Fact]
    public async Task Comment_theme_emits_when_occurrence_count_meets_minimum()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();
        DateTime utc = new(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc);

        string commentText = "Missing section on costs";

        RepeatedCommentTheme repeated = new()
        {
            ThemeKey = commentText,
            OccurrenceCount = 3,
            FirstSeenUtc = utc,
            LastSeenUtc = utc.AddHours(1),
            SampleCommentShort = commentText
        };

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope, FeedbackRollups = [], ArtifactTrends = [], RepeatedCommentThemes = [repeated]
        };

        List<ProductLearningPilotSignalRecord> signals =
        [
            Signal(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                ProductLearningDispositionValues.Rejected,
                null,
                ProductLearningSubjectTypeValues.RunOutput,
                null,
                commentText,
                null,
                utc),
            Signal(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                ProductLearningDispositionValues.Rejected,
                null,
                ProductLearningSubjectTypeValues.RunOutput,
                null,
                commentText,
                null,
                utc.AddMinutes(1)),
            Signal(
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                ProductLearningDispositionValues.Rejected,
                null,
                ProductLearningSubjectTypeValues.RunOutput,
                null,
                commentText,
                null,
                utc.AddMinutes(2))
        ];

        IReadOnlyList<ImprovementThemeWithEvidence> themes = await svc.ExtractThemesAsync(
            snapshot,
            signals,
            null,
            new ImprovementThemeExtractionOptions { MinCommentOccurrences = 2 },
            CancellationToken.None);

        ImprovementThemeWithEvidence c = Assert.Single(
            themes,
            x => x.CanonicalKey.StartsWith("comment:", StringComparison.Ordinal));

        Assert.Equal("comment:" + commentText, c.CanonicalKey);
        Assert.Equal(3, c.Theme.EvidenceCount);
        Assert.Equal(3, c.ExampleEvidence.Count);
    }

    [Fact]
    public async Task Max_themes_drops_lower_evidence_after_ranking()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();
        DateTime utc = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

        FeedbackAggregate smallRollup = new()
        {
            AggregateKey = "small",
            PatternKey = "small",
            SubjectTypeOrWorkflowArea = ProductLearningSubjectTypeValues.RunOutput,
            DistinctRunCount = 1,
            TotalSignalCount = SmallAggregatePilotSignalCount,
            TrustedCount = 0,
            RejectedCount = 2,
            RevisedCount = 0,
            NeedsFollowUpCount = 0,
            FirstSignalRecordedUtc = utc,
            LastSignalRecordedUtc = utc
        };

        FeedbackAggregate bigRollup = new()
        {
            AggregateKey = "big",
            PatternKey = "big",
            SubjectTypeOrWorkflowArea = ProductLearningSubjectTypeValues.RunOutput,
            DistinctRunCount = 3,
            TotalSignalCount = BigAggregatePilotSignalCount,
            TrustedCount = 0,
            RejectedCount = 5,
            RevisedCount = 0,
            NeedsFollowUpCount = 3,
            FirstSignalRecordedUtc = utc,
            LastSignalRecordedUtc = utc
        };

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope,
            FeedbackRollups = [smallRollup, bigRollup],
            ArtifactTrends = [],
            RepeatedCommentThemes = []
        };

        List<ProductLearningPilotSignalRecord> signals = [];

        for (int i = 0; i < SmallAggregatePilotSignalCount; i++)
        {
            signals.Add(
                Signal(
                    Guid.Parse($"66666666-6666-6666-6666-{i:D12}"),
                    ProductLearningDispositionValues.Rejected,
                    "small",
                    ProductLearningSubjectTypeValues.RunOutput,
                    null,
                    null,
                    null,
                    utc.AddMinutes(i)));
        }

        for (int i = 0; i < BigAggregatePilotSignalCount; i++)
        {
            signals.Add(
                Signal(
                    Guid.Parse($"77777777-7777-7777-7777-{i:D12}"),
                    ProductLearningDispositionValues.Rejected,
                    "big",
                    ProductLearningSubjectTypeValues.RunOutput,
                    null,
                    null,
                    null,
                    utc.AddMinutes(10 + i)));
        }

        IReadOnlyList<ImprovementThemeWithEvidence> themes = await svc.ExtractThemesAsync(
            snapshot,
            signals,
            null,
            new ImprovementThemeExtractionOptions
            {
                MinSignalsPerAggregateTheme = SmallAggregatePilotSignalCount, MaxThemes = 1
            },
            CancellationToken.None);

        ImprovementThemeWithEvidence only = Assert.Single(themes);

        Assert.Equal("rollup:big", only.CanonicalKey);
        Assert.Equal(BigAggregatePilotSignalCount, only.Theme.EvidenceCount);
    }
}
