using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Advisory;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IArchitectureDigestRepository"/>.
/// Subclass once with an InMemory implementation and once with Dapper + SQL Server
/// to guarantee both behave identically.
/// </summary>
public abstract class ArchitectureDigestRepositoryContractTests
{
    protected abstract IArchitectureDigestRepository CreateRepository();

    /// <summary>No-op for in-memory implementations; Dapper + SQL Server subclasses skip when no instance is available.</summary>
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    private static readonly Guid TenantId = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1");
    private static readonly Guid WorkspaceId = Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2");
    private static readonly Guid ProjectId = Guid.Parse("d3d3d3d3-d3d3-d3d3-d3d3-d3d3d3d3d3d3");

    private static ArchitectureDigest CreateDigest(Guid? digestId = null, DateTime? generatedUtc = null)
    {
        return new ArchitectureDigest
        {
            DigestId = digestId ?? Guid.NewGuid(),
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            RunId = Guid.NewGuid(),
            ComparedToRunId = null,
            GeneratedUtc = generatedUtc ?? DateTime.UtcNow,
            Title = $"Digest-{Guid.NewGuid():N}",
            Summary = "Contract test summary.",
            ContentMarkdown = "# Body\n\n- item",
            MetadataJson = """{"contract":"test"}"""
        };
    }

    [SkippableFact]
    public async Task Create_then_GetById_returns_same_digest()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureDigestRepository repo = CreateRepository();
        ArchitectureDigest digest = CreateDigest();

        await repo.CreateAsync(digest, CancellationToken.None);

        ArchitectureDigest? loaded = await repo.GetByIdAsync(digest.DigestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.DigestId.Should().Be(digest.DigestId);
        loaded.TenantId.Should().Be(digest.TenantId);
        loaded.WorkspaceId.Should().Be(digest.WorkspaceId);
        loaded.ProjectId.Should().Be(digest.ProjectId);
        loaded.RunId.Should().Be(digest.RunId);
        loaded.Title.Should().Be(digest.Title);
        loaded.Summary.Should().Be(digest.Summary);
        loaded.ContentMarkdown.Should().Be(digest.ContentMarkdown);
        loaded.MetadataJson.Should().Be(digest.MetadataJson);
    }

    [SkippableFact]
    public async Task GetById_nonexistent_returns_null()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureDigestRepository repo = CreateRepository();

        ArchitectureDigest? result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task ListByScope_returns_only_matching_scope()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureDigestRepository repo = CreateRepository();
        ArchitectureDigest matching = CreateDigest();
        ArchitectureDigest otherProject = CreateDigest();
        otherProject.ProjectId = Guid.NewGuid();

        await repo.CreateAsync(matching, CancellationToken.None);
        await repo.CreateAsync(otherProject, CancellationToken.None);

        IReadOnlyList<ArchitectureDigest> result = await repo.ListByScopeAsync(
            TenantId, WorkspaceId, ProjectId, take: 100, CancellationToken.None);

        result.Should().Contain(d => d.DigestId == matching.DigestId);
        result.Should().NotContain(d => d.DigestId == otherProject.DigestId);
    }

    [SkippableFact]
    public async Task ListByScope_orders_by_GeneratedUtc_descending()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureDigestRepository repo = CreateRepository();
        ArchitectureDigest older = CreateDigest(generatedUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        ArchitectureDigest newer = CreateDigest(generatedUtc: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        await repo.CreateAsync(older, CancellationToken.None);
        await repo.CreateAsync(newer, CancellationToken.None);

        IReadOnlyList<ArchitectureDigest> result = await repo.ListByScopeAsync(
            TenantId, WorkspaceId, ProjectId, take: 100, CancellationToken.None);

        List<ArchitectureDigest> ours = result
            .Where(d => d.DigestId == older.DigestId || d.DigestId == newer.DigestId)
            .ToList();
        ours.Should().HaveCountGreaterThanOrEqualTo(2);
        ours[0].DigestId.Should().Be(newer.DigestId);
        ours[1].DigestId.Should().Be(older.DigestId);
    }

    [SkippableFact]
    public async Task ListByScope_respects_take_limit()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureDigestRepository repo = CreateRepository();

        // Use a unique scope so only our data is counted.
        Guid uniqueProject = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            ArchitectureDigest d = CreateDigest();
            d.ProjectId = uniqueProject;
            await repo.CreateAsync(d, CancellationToken.None);
        }

        IReadOnlyList<ArchitectureDigest> result = await repo.ListByScopeAsync(
            TenantId, WorkspaceId, uniqueProject, take: 3, CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [SkippableFact]
    public async Task ListByScope_empty_scope_returns_empty_list()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureDigestRepository repo = CreateRepository();

        IReadOnlyList<ArchitectureDigest> result = await repo.ListByScopeAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), take: 10, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
