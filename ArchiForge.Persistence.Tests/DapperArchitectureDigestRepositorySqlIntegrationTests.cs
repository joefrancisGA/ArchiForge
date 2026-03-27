using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Connections;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// <see cref="DapperArchitectureDigestRepository"/> against real SQL Server (Docker) + production-shaped DDL from DbUp.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperArchitectureDigestRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task Create_GetById_ListByScope_round_trips_on_sql_server()
    {
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperArchitectureDigestRepository repository = new(factory);

        Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid workspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid projectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Guid digestId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        ArchitectureDigest created = new()
        {
            DigestId = digestId,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            ComparedToRunId = null,
            GeneratedUtc = new DateTime(2026, 3, 27, 10, 0, 0, DateTimeKind.Utc),
            Title = "SQL integration digest",
            Summary = "Short summary for persistence test.",
            ContentMarkdown = "# Body\n\n- item",
            MetadataJson = """{"test":true}"""
        };

        await repository.CreateAsync(created, CancellationToken.None);

        ArchitectureDigest? loaded = await repository.GetByIdAsync(digestId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.DigestId.Should().Be(digestId);
        loaded.Title.Should().Be(created.Title);
        loaded.Summary.Should().Be(created.Summary);
        loaded.ContentMarkdown.Should().Be(created.ContentMarkdown);
        loaded.MetadataJson.Should().Be(created.MetadataJson);
        loaded.TenantId.Should().Be(tenantId);
        loaded.WorkspaceId.Should().Be(workspaceId);
        loaded.ProjectId.Should().Be(projectId);
        loaded.RunId.Should().Be(created.RunId);

        IReadOnlyList<ArchitectureDigest> list = await repository.ListByScopeAsync(
            tenantId,
            workspaceId,
            projectId,
            take: 10,
            CancellationToken.None);

        list.Should().ContainSingle(d => d.DigestId == digestId);
    }
}
