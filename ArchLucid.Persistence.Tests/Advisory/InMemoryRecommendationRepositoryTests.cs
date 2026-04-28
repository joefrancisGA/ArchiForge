using ArchLucid.Decisioning.Advisory.Workflow;

namespace ArchLucid.Persistence.Tests.Advisory;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class InMemoryRecommendationRepositoryTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Guid RunId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly DateTime BaseUtc = new(2026, 4, 3, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpsertAsync_inserts_new_row_and_GetByIdAsync_returns_it()
    {
        InMemoryRecommendationRepository repo = new();
        Guid id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        RecommendationRecord rec = Minimal(id, RunId, 5, BaseUtc);

        await repo.UpsertAsync(rec, CancellationToken.None);

        RecommendationRecord? loaded = await repo.GetByIdAsync(id, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.RecommendationId.Should().Be(id);
        loaded.PriorityScore.Should().Be(5);
    }

    [Fact]
    public async Task UpsertAsync_replaces_existing_by_RecommendationId()
    {
        InMemoryRecommendationRepository repo = new();
        Guid id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await repo.UpsertAsync(Minimal(id, RunId, 1, BaseUtc, status: RecommendationStatus.Proposed),
            CancellationToken.None);

        RecommendationRecord next = Minimal(id, RunId, 9, BaseUtc.AddHours(1), status: RecommendationStatus.Accepted);
        next.Title = "updated";

        await repo.UpsertAsync(next, CancellationToken.None);

        IReadOnlyList<RecommendationRecord> scope =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, null, 50, CancellationToken.None);

        scope.Should().ContainSingle();
        scope[0].Status.Should().Be(RecommendationStatus.Accepted);
        scope[0].Title.Should().Be("updated");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        InMemoryRecommendationRepository repo = new();

        RecommendationRecord? loaded =
            await repo.GetByIdAsync(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), CancellationToken.None);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_trims_fifo_when_exceeding_MaxEntries_5000()
    {
        InMemoryRecommendationRepository repo = new();
        Guid firstId = Guid.Parse("60000000-0000-0000-0000-000000000000");

        foreach (int i in Enumerable.Range(0, 5000))
        {
            Guid id = i == 0 ? firstId : Guid.Parse($"60000000-0000-0000-0000-{i:D12}");
            await repo.UpsertAsync(Minimal(id, RunId, 0, BaseUtc.AddMinutes(i)), CancellationToken.None);
        }

        Guid newest = Guid.Parse("60000000-0000-0000-0000-000000050000");
        await repo.UpsertAsync(Minimal(newest, RunId, 0, BaseUtc.AddHours(100)), CancellationToken.None);

        RecommendationRecord? removed = await repo.GetByIdAsync(firstId, CancellationToken.None);
        removed.Should().BeNull();

        RecommendationRecord? tail = await repo.GetByIdAsync(newest, CancellationToken.None);
        tail.Should().NotBeNull();
    }

    [Fact]
    public async Task ListByRunAsync_orders_by_PriorityScore_desc_then_CreatedUtc_desc_capped_at_500()
    {
        InMemoryRecommendationRepository repo = new();
        await repo.UpsertAsync(
            Minimal(Guid.Parse("70000000-0000-0000-0000-000000000001"), RunId, 10, BaseUtc),
            CancellationToken.None);

        await repo.UpsertAsync(
            Minimal(Guid.Parse("70000000-0000-0000-0000-000000000002"), RunId, 20, BaseUtc.AddMinutes(1)),
            CancellationToken.None);

        await repo.UpsertAsync(
            Minimal(Guid.Parse("70000000-0000-0000-0000-000000000003"), RunId, 20, BaseUtc.AddMinutes(5)),
            CancellationToken.None);

        IReadOnlyList<RecommendationRecord> list =
            await repo.ListByRunAsync(TenantId, WorkspaceId, ProjectId, RunId, CancellationToken.None);

        list.Should().HaveCount(3);
        list[0].RecommendationId.Should().Be(Guid.Parse("70000000-0000-0000-0000-000000000003"));
        list[1].RecommendationId.Should().Be(Guid.Parse("70000000-0000-0000-0000-000000000002"));
        list[2].RecommendationId.Should().Be(Guid.Parse("70000000-0000-0000-0000-000000000001"));

        Task[] many = Enumerable
            .Range(0, 501)
            .Select(i => repo.UpsertAsync(
                Minimal(Guid.Parse($"71000000-0000-0000-0000-{i:000000000000}"), RunId, 1, BaseUtc.AddHours(i + 10)),
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(many);

        IReadOnlyList<RecommendationRecord> capped =
            await repo.ListByRunAsync(TenantId, WorkspaceId, ProjectId, RunId, CancellationToken.None);

        capped.Should().HaveCount(500);
    }

    [Fact]
    public async Task ListByScopeAsync_filters_status_exact_optional_orders_LastUpdatedUtc_desc_and_clamps_take()
    {
        InMemoryRecommendationRepository repo = new();
        await repo.UpsertAsync(
            Minimal(Guid.Parse("72000000-0000-0000-0000-000000000001"), RunId, 1, BaseUtc, BaseUtc),
            CancellationToken.None);

        await repo.UpsertAsync(
            Minimal(
                Guid.Parse("72000000-0000-0000-0000-000000000002"),
                RunId,
                1,
                BaseUtc,
                BaseUtc.AddDays(2),
                RecommendationStatus.Accepted),
            CancellationToken.None);

        await repo.UpsertAsync(
            Minimal(
                Guid.Parse("72000000-0000-0000-0000-000000000003"),
                RunId,
                1,
                BaseUtc,
                BaseUtc.AddDays(1)),
            CancellationToken.None);

        IReadOnlyList<RecommendationRecord> all =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, null, 10, CancellationToken.None);

        all.Should().HaveCount(3);
        all[0].RecommendationId.Should().Be(Guid.Parse("72000000-0000-0000-0000-000000000002"));

        IReadOnlyList<RecommendationRecord> proposedOnly =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, RecommendationStatus.Proposed, 10,
                CancellationToken.None);

        proposedOnly.Should().HaveCount(2);

        IReadOnlyList<RecommendationRecord> defaultTake =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, null, 0, CancellationToken.None);

        defaultTake.Should().HaveCount(3);

        IReadOnlyList<RecommendationRecord> maxCap =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, null, 900, CancellationToken.None);

        maxCap.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpsertAsync_with_null_recommendation_throws()
    {
        InMemoryRecommendationRepository repo = new();

        Func<Task> act = async () => await repo.UpsertAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static RecommendationRecord Minimal(
        Guid recommendationId,
        Guid runId,
        int priorityScore,
        DateTime createdUtc,
        DateTime? lastUpdated = null,
        string status = RecommendationStatus.Proposed)
    {
        return new RecommendationRecord
        {
            RecommendationId = recommendationId,
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            RunId = runId,
            Title = "t",
            Category = "c",
            Rationale = "r",
            SuggestedAction = "s",
            Urgency = "u",
            ExpectedImpact = "e",
            PriorityScore = priorityScore,
            Status = status,
            CreatedUtc = createdUtc,
            LastUpdatedUtc = lastUpdated ?? createdUtc
        };
    }
}
