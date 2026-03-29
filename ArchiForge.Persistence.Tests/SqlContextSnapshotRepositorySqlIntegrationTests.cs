using ArchiForge.ContextIngestion.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Serialization;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// <see cref="SqlContextSnapshotRepository"/> against SQL Server + DbUp (relational children + JSON dual-write / read fallback).
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
        SqlContextSnapshotRepository repository = new(factory);

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
                        ["region"] = "east",
                        ["tier"] = "p1"
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
        SqlContextSnapshotRepository repository = new(factory);

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
        string errorsJson = JsonEntitySerializer.Serialize(new List<string>());
        string hashesJson = JsonEntitySerializer.Serialize(new Dictionary<string, string>());

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
        loaded.Errors.Should().BeEmpty();
        loaded.SourceHashes.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task SaveAsync_with_explicit_transaction_commits_header_and_children()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlContextSnapshotRepository repository = new(factory);

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
