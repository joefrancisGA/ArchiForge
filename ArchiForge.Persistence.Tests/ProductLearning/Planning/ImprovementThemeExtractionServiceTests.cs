using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;
using ArchiForge.Persistence.ProductLearning.Planning;

namespace ArchiForge.Persistence.Tests.ProductLearning.Planning;

public sealed class ImprovementThemeExtractionServiceTests
{
    private static ProductLearningScope Scope() =>
        new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };

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
            RecordedUtc = utc,
        };
    }

    [Fact]
    public async Task Extract_empty_inputs_yields_empty()
    {
        ImprovementThemeExtractionService svc = new();
        ProductLearningScope scope = Scope();

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope,
            FeedbackRollups = Array.Empty<FeedbackAggregate>(),
            ArtifactTrends = Array.Empty<ArtifactOutcomeTrend>(),
            RepeatedCommentThemes = Array.Empty<RepeatedCommentTheme>(),
        };

        IReadOnlyList<ImprovementThemeWithEvidence> themes = await svc.ExtractThemesAsync(
            snapshot,
            Array.Empty<ProductLearningPilotSignalRecord>(),
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
            LastSignalRecordedUtc = utc.AddHours(1),
        };

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope,
            FeedbackRollups = new[] { aggregate },
            ArtifactTrends = Array.Empty<ArtifactOutcomeTrend>(),
            RepeatedCommentThemes = Array.Empty<RepeatedCommentTheme>(),
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
                utc.AddMinutes(2)),
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
            Scope = scope,
            FeedbackRollups = Array.Empty<FeedbackAggregate>(),
            ArtifactTrends = Array.Empty<ArtifactOutcomeTrend>(),
            RepeatedCommentThemes = Array.Empty<RepeatedCommentTheme>(),
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
                utc.AddMinutes(1)),
        ];

        IReadOnlyList<ImprovementThemeWithEvidence> below = await svc.ExtractThemesAsync(
            snapshot,
            signals,
            null,
            new ImprovementThemeExtractionOptions { MinTagOccurrences = 3 },
            CancellationToken.None);

        Assert.DoesNotContain(below, t => t.CanonicalKey.StartsWith("tag:", StringComparison.Ordinal));

        List<ProductLearningPilotSignalRecord> three = new(signals)
        {
            Signal(
                Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                ProductLearningDispositionValues.Trusted,
                null,
                ProductLearningSubjectTypeValues.RunOutput,
                "diagram",
                null,
                detail,
                utc.AddMinutes(2)),
        };

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
            Scope = scope,
            FeedbackRollups = Array.Empty<FeedbackAggregate>(),
            ArtifactTrends = Array.Empty<ArtifactOutcomeTrend>(),
            RepeatedCommentThemes = Array.Empty<RepeatedCommentTheme>(),
        };

        ProductLearningPilotSignalRecord badScope = new()
        {
            SignalId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            SubjectType = ProductLearningSubjectTypeValues.RunOutput,
            Disposition = ProductLearningDispositionValues.Trusted,
            RecordedUtc = DateTime.UtcNow,
        };

        await Assert.ThrowsAsync<ArgumentException>(() => svc.ExtractThemesAsync(
            snapshot,
            new[] { badScope },
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
            LastSignalRecordedUtc = utc,
        };

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope,
            FeedbackRollups = new[] { aggregate },
            ArtifactTrends = Array.Empty<ArtifactOutcomeTrend>(),
            RepeatedCommentThemes = Array.Empty<RepeatedCommentTheme>(),
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
                utc),
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
}
