using ArchLucid.Contracts.Manifest;
using ArchLucid.Persistence.Data.Repositories;

using ArchLucid.Persistence.Tests.Support;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
/// After ADR 0030 PR A4, the SQL <see cref="GoldenManifestRepository"/> no longer targets <c>dbo.GoldenManifestVersions</c>
/// (table dropped in migration 111). Coordinator-shaped persistence for SQL flows is via <c>dbo.GoldenManifests</c> on the
/// Authority path; this type exists only as a DI placeholder that fails fast on writes.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperCoordinatorGoldenManifestRepositoryContractTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task Sql_coordinator_golden_manifest_create_throws_after_legacy_table_removal()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        ICoordinatorGoldenManifestRepository repo = new GoldenManifestRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
        GoldenManifest manifest = new()
        {
            RunId = Guid.NewGuid().ToString("N"),
            SystemName = "s",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata
            {
                ManifestVersion = "v-coord-retired",
                CreatedUtc = DateTime.UtcNow,
            },
        };

        Func<Task> act = async () => await repo.CreateAsync(manifest, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*GoldenManifestVersions*");
    }

    [SkippableFact]
    public async Task Sql_coordinator_golden_manifest_get_returns_null()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        ICoordinatorGoldenManifestRepository repo = new GoldenManifestRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));

        GoldenManifest? loaded = await repo.GetByVersionAsync("any-version-" + Guid.NewGuid().ToString("N"), CancellationToken.None);

        loaded.Should().BeNull();
    }
}
