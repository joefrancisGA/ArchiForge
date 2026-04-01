using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Persistence.ProductLearning;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.ProductLearning;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryProductLearningPilotSignalRepositoryTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Insert_then_list_returns_newest_first_with_stable_secondary_sort()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();
        DateTime t0 = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime t1 = new(2026, 4, 1, 12, 0, 1, DateTimeKind.Utc);

        await repo.InsertAsync(
            new ProductLearningPilotSignalRecord
            {
                TenantId = TenantId,
                WorkspaceId = WorkspaceId,
                ProjectId = ProjectId,
                SubjectType = ProductLearningSubjectTypeValues.RunOutput,
                Disposition = ProductLearningDispositionValues.Trusted,
                RecordedUtc = t0,
                SignalId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            },
            CancellationToken.None);

        await repo.InsertAsync(
            new ProductLearningPilotSignalRecord
            {
                TenantId = TenantId,
                WorkspaceId = WorkspaceId,
                ProjectId = ProjectId,
                SubjectType = ProductLearningSubjectTypeValues.ManifestArtifact,
                Disposition = ProductLearningDispositionValues.Rejected,
                PatternKey = "diagram.layout",
                RecordedUtc = t1,
                SignalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            },
            CancellationToken.None);

        IReadOnlyList<ProductLearningPilotSignalRecord> list =
            await repo.ListRecentForScopeAsync(TenantId, WorkspaceId, ProjectId, 10, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].Disposition.Should().Be(ProductLearningDispositionValues.Rejected);
        list[1].Disposition.Should().Be(ProductLearningDispositionValues.Trusted);
    }

    [Fact]
    public async Task Insert_assigns_id_and_utc_when_defaults()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        await repo.InsertAsync(
            new ProductLearningPilotSignalRecord
            {
                TenantId = TenantId,
                WorkspaceId = WorkspaceId,
                ProjectId = ProjectId,
                SubjectType = ProductLearningSubjectTypeValues.Other,
                Disposition = ProductLearningDispositionValues.NeedsFollowUp,
            },
            CancellationToken.None);

        IReadOnlyList<ProductLearningPilotSignalRecord> list =
            await repo.ListRecentForScopeAsync(TenantId, WorkspaceId, ProjectId, 5, CancellationToken.None);

        list.Should().ContainSingle();
        list[0].SignalId.Should().NotBe(Guid.Empty);
        list[0].RecordedUtc.Should().BeAfter(DateTime.MinValue);
        list[0].TriageStatus.Should().Be(ProductLearningTriageStatusValues.Open);
    }

    [Fact]
    public async Task Insert_without_subject_throws()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        Func<Task> act = async () => await repo.InsertAsync(
            new ProductLearningPilotSignalRecord
            {
                TenantId = TenantId,
                WorkspaceId = WorkspaceId,
                ProjectId = ProjectId,
                SubjectType = "",
                Disposition = ProductLearningDispositionValues.Trusted,
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListRunFeedbackAggregatesAsync_groups_by_pattern_key_and_counts_dispositions()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        await repo.InsertAsync(
            Signal(
                ProductLearningDispositionValues.Trusted,
                patternKey: "cost.section",
                runId: "run-a",
                comment: null),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(
                ProductLearningDispositionValues.Rejected,
                patternKey: "cost.section",
                runId: "run-b",
                comment: "too vague"),
            CancellationToken.None);

        IReadOnlyList<FeedbackAggregate> agg =
            await repo.ListRunFeedbackAggregatesAsync(TenantId, WorkspaceId, ProjectId, sinceUtc: null, 50, CancellationToken.None);

        agg.Should().ContainSingle();
        agg[0].AggregateKey.Should().Be("cost.section");
        agg[0].TotalSignalCount.Should().Be(2);
        agg[0].DistinctRunCount.Should().Be(2);
        agg[0].TrustedCount.Should().Be(1);
        agg[0].RejectedCount.Should().Be(1);
    }

    [Fact]
    public async Task ListTopRejectedRevisedArtifactRollupsAsync_prefers_high_reject_plus_revised()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Rejected, patternKey: "a", runId: "r1"),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Revised, patternKey: "a", runId: "r2"),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Rejected, patternKey: "b", runId: "r3"),
            CancellationToken.None);

        IReadOnlyList<FeedbackAggregate> top =
            await repo.ListTopRejectedRevisedArtifactRollupsAsync(
                TenantId,
                WorkspaceId,
                ProjectId,
                sinceUtc: null,
                take: 5,
                CancellationToken.None);

        top.Should().HaveCount(2);

        int score0 = top[0].RejectedCount + top[0].RevisedCount;
        int score1 = top[1].RejectedCount + top[1].RevisedCount;
        score0.Should().BeGreaterThan(score1);
    }

    [Fact]
    public async Task ListRepeatedCommentThemesAsync_groups_trimmed_prefix_deterministically()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        string longComment = new string('x', 250);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Trusted, patternKey: "p1", runId: "r1", comment: longComment),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Trusted, patternKey: "p2", runId: "r2", comment: longComment + "tail"),
            CancellationToken.None);

        IReadOnlyList<RepeatedCommentTheme> themes =
            await repo.ListRepeatedCommentThemesAsync(
                TenantId,
                WorkspaceId,
                ProjectId,
                sinceUtc: null,
                minOccurrences: 2,
                take: 10,
                CancellationToken.None);

        themes.Should().ContainSingle();
        themes[0].OccurrenceCount.Should().Be(2);
        themes[0].ThemeKey.Length.Should().Be(ProductLearningSignalAggregations.CommentThemePrefixLength);
    }

    [Fact]
    public async Task ListImprovementOpportunityCandidatesAsync_respects_poor_outcome_threshold()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Trusted, patternKey: "ok", runId: "r1"),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Rejected, patternKey: "bad", runId: "r2"),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.NeedsFollowUp, patternKey: "bad", runId: "r3"),
            CancellationToken.None);

        IReadOnlyList<ImprovementOpportunity> opps =
            await repo.ListImprovementOpportunityCandidatesAsync(
                TenantId,
                WorkspaceId,
                ProjectId,
                sinceUtc: null,
                minPoorOutcomeSignals: 2,
                minRevisedSignals: 5,
                take: 10,
                CancellationToken.None);

        opps.Should().ContainSingle();
        opps[0].SourceAggregateKey.Should().Be("bad");
    }

    private ProductLearningPilotSignalRecord Signal(
        string disposition,
        string patternKey,
        string runId,
        string? comment = null)
    {
        return new ProductLearningPilotSignalRecord
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            SubjectType = ProductLearningSubjectTypeValues.RunOutput,
            Disposition = disposition,
            PatternKey = patternKey,
            ArchitectureRunId = runId,
            CommentShort = comment,
            RecordedUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }
}
