using ArchLucid.Decisioning.Advisory.Learning;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Advisory;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class InMemoryRecommendationLearningProfileRepositoryTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateTime SampleUtc = new(2026, 4, 4, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetLatestAsync_returns_null_when_empty()
    {
        InMemoryRecommendationLearningProfileRepository repo = new();

        RecommendationLearningProfile? latest =
            await repo.GetLatestAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        latest.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_then_GetLatestAsync_returns_that_profile()
    {
        InMemoryRecommendationLearningProfileRepository repo = new();
        DateTime generated = new(2026, 4, 4, 12, 0, 0, DateTimeKind.Utc);
        RecommendationLearningProfile profile = BuildProfile(generated);

        await repo.SaveAsync(profile, CancellationToken.None);

        RecommendationLearningProfile? latest =
            await repo.GetLatestAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        latest.Should().NotBeNull();
        latest.GeneratedUtc.Should().Be(generated);
    }

    [Fact]
    public async Task GetLatestAsync_returns_most_recent_GeneratedUtc_for_scope()
    {
        InMemoryRecommendationLearningProfileRepository repo = new();
        await repo.SaveAsync(BuildProfile(new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);
        await repo.SaveAsync(BuildProfile(new DateTime(2026, 4, 4, 15, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);
        await repo.SaveAsync(BuildProfile(new DateTime(2026, 4, 4, 12, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        RecommendationLearningProfile? latest =
            await repo.GetLatestAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        latest.Should().NotBeNull();
        latest.GeneratedUtc.Should().Be(new DateTime(2026, 4, 4, 15, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetLatestAsync_ignores_other_scopes()
    {
        InMemoryRecommendationLearningProfileRepository repo = new();
        await repo.SaveAsync(BuildProfile(new DateTime(2026, 4, 4, 20, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        RecommendationLearningProfile otherTenant = BuildProfile(new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc));
        otherTenant.TenantId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        await repo.SaveAsync(otherTenant, CancellationToken.None);

        RecommendationLearningProfile? latest =
            await repo.GetLatestAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        latest.Should().NotBeNull();
        latest.GeneratedUtc.Should().Be(new DateTime(2026, 4, 4, 20, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task SaveAsync_trims_oldest_inserted_row_when_exceeding_500()
    {
        InMemoryRecommendationLearningProfileRepository repo = new();
        DateTime sentinel = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await repo.SaveAsync(BuildProfile(sentinel), CancellationToken.None);

        Task[] tail = Enumerable
            .Range(1, 500)
            .Select(i => repo.SaveAsync(
                BuildProfile(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i)),
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(tail);

        RecommendationLearningProfile? latest =
            await repo.GetLatestAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        latest.Should().NotBeNull();
        latest.GeneratedUtc.Should().Be(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(500));
        latest.GeneratedUtc.Should().NotBe(sentinel);
    }

    [Fact]
    public async Task SaveAsync_throws_when_cancellation_requested_before_lock()
    {
        InMemoryRecommendationLearningProfileRepository repo = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> act = async () => await repo.SaveAsync(BuildProfile(SampleUtc), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static RecommendationLearningProfile BuildProfile(DateTime generatedUtc)
    {
        return new RecommendationLearningProfile
        {
            TenantId = TenantId, WorkspaceId = WorkspaceId, ProjectId = ProjectId, GeneratedUtc = generatedUtc
        };
    }
}
