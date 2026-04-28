using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Tests.ProductLearning;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
[Trait("ChangeSet", "58R")]
public sealed class ProductLearningFeedbackAggregationServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ProductLearningScope Scope()
    {
        return new ProductLearningScope { TenantId = TenantId, WorkspaceId = WorkspaceId, ProjectId = ProjectId };
    }

    [Fact]
    public async Task GetSnapshotAsync_drops_rollups_below_MinSignalsPerAggregate()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        await repo.InsertAsync(
            Signal("solo-pattern", new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), "run-a"),
            CancellationToken.None);

        ProductLearningFeedbackAggregationService svc = new(repo);
        ProductLearningTriageOptions options = new()
        {
            MinSignalsPerAggregate = 2,
            MaxFeedbackRollups = 50,
            MaxArtifactTrends = 50
        };

        ProductLearningAggregationSnapshot snap =
            await svc.GetSnapshotAsync(Scope(), options, CancellationToken.None);

        snap.FeedbackRollups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSnapshotAsync_respects_SinceUtc_when_building_rollups()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();
        DateTime before = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime after = new(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);

        await repo.InsertAsync(Signal("shared", before, "run-old"), CancellationToken.None);
        await repo.InsertAsync(Signal("shared", after, "run-new"), CancellationToken.None);

        ProductLearningFeedbackAggregationService svc = new(repo);
        ProductLearningTriageOptions options = new()
        {
            SinceUtc = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
            MinSignalsPerAggregate = 2,
            MaxFeedbackRollups = 50,
            MaxArtifactTrends = 50
        };

        ProductLearningAggregationSnapshot snap =
            await svc.GetSnapshotAsync(Scope(), options, CancellationToken.None);

        snap.FeedbackRollups.Should().BeEmpty();
        snap.SinceUtc.Should().Be(options.SinceUtc);
    }

    [Fact]
    public async Task GetSnapshotAsync_does_not_query_top_rejected_revised_slice()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();
        DateTime utc = new(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Rejected, "pat", utc, "run-1"),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Revised, "pat", utc, "run-2"),
            CancellationToken.None);

        ProductLearningFeedbackAggregationService svc = new(repo);
        ProductLearningTriageOptions options = new()
        {
            MinSignalsPerAggregate = 1,
            MaxFeedbackRollups = 50,
            MaxArtifactTrends = 50,
            MinNegativeOutcomesOnArtifactTrend = 99
        };

        ProductLearningAggregationSnapshot snap =
            await svc.GetSnapshotAsync(Scope(), options, CancellationToken.None);

        snap.FeedbackRollups.Should().NotBeEmpty();
        snap.TopRejectedRevisedRollups.Should().BeEmpty();
    }

    private static ProductLearningPilotSignalRecord Signal(string patternKey, DateTime recordedUtc, string runId)
    {
        return Signal(ProductLearningDispositionValues.Trusted, patternKey, recordedUtc, runId);
    }

    private static ProductLearningPilotSignalRecord Signal(
        string disposition,
        string patternKey,
        DateTime recordedUtc,
        string runId)
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
            RecordedUtc = recordedUtc
        };
    }
}
