using ArchLucid.ContextIngestion.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;

using static ArchLucid.Persistence.Tests.Support.PersistenceIntegrationTestScope;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="SqlContextSnapshotRepository" /> against SQL Server + DbUp (relational children + JSON dual-write / read
///     fallback).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlContextSnapshotRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task Save_then_GetById_round_trips_relational_collections()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlContextSnapshotRepository repository = new(factory, Empty);

        Guid snapshotId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        DateTime created = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);

        ContextSnapshot snapshot = new()
        {
            SnapshotId = snapshotId,
            RunId = runId,
            ProjectId = "proj-relational-1",
            CreatedUtc = created,
            DeltaSummary = "delta",
            CanonicalObjects =
            [
                new CanonicalObject
                {
                    ObjectId = "obj-1",
                    ObjectType = "Service",
                    Name = "Api",
                    SourceType = "Request",
                    SourceId = "src-1",
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["region"] = "east", ["tier"] = "p1"
                    }
                }
            ],
            Warnings = ["w1", "w2"],
            Errors = ["e1"],
            SourceHashes = new Dictionary<string, string>(StringComparer.Ordinal) { ["file.cs"] = "abc123" }
        };

        await repository.SaveAsync(snapshot, CancellationToken.None);

        ContextSnapshot? loaded = await repository.GetByIdAsync(snapshotId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.SnapshotId.Should().Be(snapshotId);
        loaded.RunId.Should().Be(runId);
        loaded.ProjectId.Should().Be("proj-relational-1");
        loaded.DeltaSummary.Should().Be("delta");
        loaded.CanonicalObjects.Should().ContainSingle();
        loaded.CanonicalObjects[0].ObjectId.Should().Be("obj-1");
        loaded.CanonicalObjects[0].Properties.Should().HaveCount(2);
        loaded.CanonicalObjects[0].Properties["region"].Should().Be("east");
        loaded.Warnings.Should().Equal("w1", "w2");
        loaded.Errors.Should().Equal("e1");
        loaded.SourceHashes["file.cs"].Should().Be("abc123");
    }

    [SkippableFact]
    public async Task GetById_falls_back_to_json_when_no_relational_child_rows()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlContextSnapshotRepository repository = new(factory, Empty);

        Guid snapshotId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();

        List<CanonicalObject> canonical =
        [
            new()
            {
                ObjectId = "legacy-obj",
                ObjectType = "Type",
                Name = "Legacy",
                SourceType = "S",
                SourceId = "sid",
                Properties = []
            }
        ];

        string canonicalJson = JsonEntitySerializer.Serialize(canonical);
        string warningsJson = JsonEntitySerializer.Serialize(new List<string> { "jw" });
        string errorsJson = JsonEntitySerializer.Serialize(new List<string> { "err-a", "err-b" });
        string hashesJson = JsonEntitySerializer.Serialize(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["legacy/path.cs"] = "sha256:aa", ["other"] = "bb"
            });

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        const string insertHeader = """
                                    INSERT INTO dbo.ContextSnapshots
                                    (
                                        SnapshotId, RunId, ProjectId, CreatedUtc,
                                        CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
                                    )
                                    VALUES
                                    (
                                        @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                                        @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    SnapshotId = snapshotId,
                    RunId = runId,
                    ProjectId = "proj-legacy-json",
                    CreatedUtc = DateTime.UtcNow,
                    CanonicalObjectsJson = canonicalJson,
                    DeltaSummary = (string?)null,
                    WarningsJson = warningsJson,
                    ErrorsJson = errorsJson,
                    SourceHashesJson = hashesJson
                },
                cancellationToken: CancellationToken.None));

        ContextSnapshot? loaded = await repository.GetByIdAsync(snapshotId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.CanonicalObjects.Should().ContainSingle(o => o.ObjectId == "legacy-obj");
        loaded.Warnings.Should().Equal("jw");
        loaded.Errors.Should().Equal("err-a", "err-b");
        loaded.SourceHashes.Should().HaveCount(2);
        loaded.SourceHashes["legacy/path.cs"].Should().Be("sha256:aa");
        loaded.SourceHashes["other"].Should().Be("bb");
    }

    [SkippableFact]
    public async Task GetById_json_fallback_deserializes_canonical_object_properties()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlContextSnapshotRepository repository = new(factory, Empty);

        Guid snapshotId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc);

        List<CanonicalObject> canonical =
        [
            new()
            {
                ObjectId = "obj-props",
                ObjectType = "Resource",
                Name = "PrimaryApi",
                SourceType = "Ingest",
                SourceId = "src-props-1",
                Properties = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["region"] = "east", ["tier"] = "premium", ["env"] = "production"
                }
            },
            new()
            {
                ObjectId = "obj-empty-props",
                ObjectType = "Service",
                Name = "NoPropsSvc",
                SourceType = "Catalog",
                SourceId = "src-empty",
                Properties = []
            }
        ];

        string canonicalJson = JsonEntitySerializer.Serialize(canonical);

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        const string insertHeader = """
                                    INSERT INTO dbo.ContextSnapshots
                                    (
                                        SnapshotId, RunId, ProjectId, CreatedUtc,
                                        CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
                                    )
                                    VALUES
                                    (
                                        @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                                        @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    SnapshotId = snapshotId,
                    RunId = runId,
                    ProjectId = "proj-canonical-props-json",
                    CreatedUtc = createdUtc,
                    CanonicalObjectsJson = canonicalJson,
                    DeltaSummary = (string?)null,
                    WarningsJson = JsonEntitySerializer.Serialize(new List<string>()),
                    ErrorsJson = JsonEntitySerializer.Serialize(new List<string>()),
                    SourceHashesJson = JsonEntitySerializer.Serialize(new Dictionary<string, string>())
                },
                cancellationToken: CancellationToken.None));

        ContextSnapshot? loaded = await repository.GetByIdAsync(snapshotId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.CanonicalObjects.Should().HaveCount(2);

        loaded.CanonicalObjects[0].ObjectId.Should().Be("obj-props");
        loaded.CanonicalObjects[0].ObjectType.Should().Be("Resource");
        loaded.CanonicalObjects[0].Name.Should().Be("PrimaryApi");
        loaded.CanonicalObjects[0].SourceType.Should().Be("Ingest");
        loaded.CanonicalObjects[0].SourceId.Should().Be("src-props-1");
        loaded.CanonicalObjects[0].Properties.Should().HaveCount(3);
        loaded.CanonicalObjects[0].Properties["region"].Should().Be("east");
        loaded.CanonicalObjects[0].Properties["tier"].Should().Be("premium");
        loaded.CanonicalObjects[0].Properties["env"].Should().Be("production");

        loaded.CanonicalObjects[1].ObjectId.Should().Be("obj-empty-props");
        loaded.CanonicalObjects[1].ObjectType.Should().Be("Service");
        loaded.CanonicalObjects[1].Name.Should().Be("NoPropsSvc");
        loaded.CanonicalObjects[1].SourceType.Should().Be("Catalog");
        loaded.CanonicalObjects[1].SourceId.Should().Be("src-empty");
        loaded.CanonicalObjects[1].Properties.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task GetById_when_all_json_columns_null_returns_empty_collections()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlContextSnapshotRepository repository = new(factory, Empty);

        Guid snapshotId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        const string insertHeader = """
                                    INSERT INTO dbo.ContextSnapshots
                                    (
                                        SnapshotId, RunId, ProjectId, CreatedUtc,
                                        CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
                                    )
                                    VALUES
                                    (
                                        @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                                        @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    SnapshotId = snapshotId,
                    RunId = runId,
                    ProjectId = "proj-all-json-null",
                    CreatedUtc = createdUtc,
                    CanonicalObjectsJson = (string?)null,
                    DeltaSummary = (string?)null,
                    WarningsJson = (string?)null,
                    ErrorsJson = (string?)null,
                    SourceHashesJson = (string?)null
                },
                cancellationToken: CancellationToken.None));

        ContextSnapshot? loaded = await repository.GetByIdAsync(snapshotId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.CanonicalObjects.Should().BeEmpty();
        loaded.Warnings.Should().BeEmpty();
        loaded.Errors.Should().BeEmpty();
        loaded.SourceHashes.Should().BeEmpty();
        loaded.DeltaSummary.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetById_when_all_json_columns_are_empty_strings_returns_empty_collections()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlContextSnapshotRepository repository = new(factory, Empty);

        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid scopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid snapshotId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 11, 11, 14, 0, 0, DateTimeKind.Utc);
        const string projectId = "proj-json-empty-string";

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        await AuthorityRunChainTestSeed.InsertRunAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            projectId,
            CancellationToken.None);

        const string insertHeader = """
                                    INSERT INTO dbo.ContextSnapshots
                                    (
                                        SnapshotId, RunId, ProjectId, CreatedUtc,
                                        CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
                                    )
                                    VALUES
                                    (
                                        @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                                        @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
                                    );
                                    """;

        string empty = "";

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    SnapshotId = snapshotId,
                    RunId = runId,
                    ProjectId = projectId,
                    CreatedUtc = createdUtc,
                    CanonicalObjectsJson = empty,
                    DeltaSummary = (string?)null,
                    WarningsJson = empty,
                    ErrorsJson = empty,
                    SourceHashesJson = empty
                },
                cancellationToken: CancellationToken.None));

        ContextSnapshot? loaded = await repository.GetByIdAsync(snapshotId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.SnapshotId.Should().Be(snapshotId);
        loaded.RunId.Should().Be(runId);
        loaded.ProjectId.Should().Be(projectId);
        loaded.CreatedUtc.Should().Be(createdUtc);
        loaded.CanonicalObjects.Should().BeEmpty();
        loaded.Warnings.Should().BeEmpty();
        loaded.Errors.Should().BeEmpty();
        loaded.SourceHashes.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task SaveAsync_with_explicit_transaction_commits_header_and_children()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlContextSnapshotRepository repository = new(factory, Empty);

        Guid snapshotId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();

        ContextSnapshot snapshot = new()
        {
            SnapshotId = snapshotId,
            RunId = runId,
            ProjectId = "proj-tx",
            CreatedUtc = DateTime.UtcNow,
            CanonicalObjects = [],
            Warnings = ["tw"],
            Errors = [],
            SourceHashes = new Dictionary<string, string>(StringComparer.Ordinal)
        };

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        await using SqlTransaction tx = connection.BeginTransaction();
        await repository.SaveAsync(snapshot, CancellationToken.None, connection, tx);
        tx.Commit();

        ContextSnapshot? loaded = await repository.GetByIdAsync(snapshotId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Warnings.Should().Equal("tw");
    }
}
