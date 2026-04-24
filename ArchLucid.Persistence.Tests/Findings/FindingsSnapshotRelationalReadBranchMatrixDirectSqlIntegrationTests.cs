using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Findings;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Findings;

/// <summary>
///     Branch matrix for <see cref="FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync" /> (severities, payload
///     codec paths, single-child-table slices, legacy empty vs populated).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class FindingsSnapshotRelationalReadBranchMatrixDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ScopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static string EmptyList<T>()
    {
        return JsonEntitySerializer.Serialize(new List<T>());
    }

    private static async
        Task<(SqlConnection Connection, Guid RunId, Guid ContextId, Guid GraphId, Guid FindingsId, DateTime CreatedUtc)>
        SeedFindingsHeaderAsync(
            SqlServerPersistenceFixture fx,
            string slug)
    {
        SqlConnectionFactory factory = new(fx.ConnectionString);
        SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc);

        await AuthorityRunChainTestSeed.SeedRunAndContextOnlyAsync(
            connection,
            TenantId,
            WorkspaceId,
            ScopeProjectId,
            runId,
            contextId,
            "proj-find-br-" + slug,
            CancellationToken.None);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GraphSnapshots
                (GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc, NodesJson, EdgesJson, WarningsJson)
                VALUES (@G, @C, @R, SYSUTCDATETIME(), @Nj, @Ej, @Wj);
                """,
                new
                {
                    G = graphId,
                    C = contextId,
                    R = runId,
                    Nj = EmptyList<GraphNode>(),
                    Ej = EmptyList<GraphEdge>(),
                    Wj = EmptyList<string>()
                },
                cancellationToken: CancellationToken.None));

        return (connection, runId, contextId, graphId, findingsId, createdUtc);
    }

    private static async Task InsertFindingsHeaderAsync(
        SqlConnection connection,
        Guid findingsId,
        Guid runId,
        Guid contextId,
        Guid graphId,
        DateTime createdUtc,
        string findingsJson)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.FindingsSnapshots
                (FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, TenantId, WorkspaceId, ProjectId,
                 CreatedUtc, SchemaVersion, FindingsJson)
                VALUES (@F, @R, @C, @G, @T, @W, @P, @Created, 1, @Fj);
                """,
                new
                {
                    F = findingsId,
                    R = runId,
                    C = contextId,
                    G = graphId,
                    T = TenantId,
                    W = WorkspaceId,
                    P = ScopeProjectId,
                    Created = createdUtc,
                    Fj = findingsJson
                },
                cancellationToken: CancellationToken.None));
    }

    [SkippableTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task LoadRelationalSnapshotAsync_branch_matrix(int branch)
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        (SqlConnection connection, Guid runId, Guid contextId, Guid graphId, Guid findingsId, DateTime createdUtc) =
            await SeedFindingsHeaderAsync(fixture, "mx" + branch);

        await using (connection)
        {
            if (branch is 0 or 1)
            {
                FindingsSnapshot legacy = new()
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    CreatedUtc = createdUtc,
                    SchemaVersion = 1,
                    Findings = branch == 0
                        ? []
                        :
                        [
                            new Finding
                            {
                                FindingId = "x",
                                FindingType = "t",
                                Category = "c",
                                EngineType = "e",
                                Severity = FindingSeverity.Info,
                                Title = "t",
                                Rationale = "r"
                            }
                        ]
                };

                await InsertFindingsHeaderAsync(connection, findingsId, runId, contextId, graphId, createdUtc,
                    JsonEntitySerializer.Serialize(legacy));

                FindingsSnapshotStorageRow row = await RowAsync(connection, findingsId);
                FindingsSnapshot snap =
                    await FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync(connection, row,
                        CancellationToken.None);

                if (branch == 0)
                {
                    snap.Findings.Should().BeEmpty();
                    return;
                }

                snap.Findings.Should().ContainSingle(f => f.FindingId == "x");
                return;
            }

            await InsertFindingsHeaderAsync(
                connection,
                findingsId,
                runId,
                contextId,
                graphId,
                createdUtc,
                JsonEntitySerializer.Serialize(new FindingsSnapshot
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    CreatedUtc = createdUtc,
                    Findings = []
                }));

            string severity = branch switch
            {
                2 => "info",
                3 => "warning",
                4 => "error",
                5 => "critical",
                6 => "INFO",
                _ => "info"
            };

            Guid recordId = Guid.NewGuid();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.FindingRecords
                    (FindingRecordId, FindingsSnapshotId, SortOrder, FindingId, FindingSchemaVersion, FindingType,
                     Category, EngineType, Severity, Title, Rationale, PayloadType, PayloadJson)
                    VALUES (@Id, @Fs, 0, N'fid', 1, N't', N'c', N'e', @Sev, N'title', N'rat', NULL, NULL);
                    """,
                    new { Id = recordId, Fs = findingsId, Sev = severity },
                    cancellationToken: CancellationToken.None));

            if (branch == 7)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.FindingRelatedNodes (FindingRecordId, SortOrder, NodeId)
                        VALUES (@Id, 0, N'n1');
                        """,
                        new { Id = recordId },
                        cancellationToken: CancellationToken.None));
            }

            if (branch == 8)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.FindingRecommendedActions (FindingRecordId, SortOrder, ActionText)
                        VALUES (@Id, 0, N'act');
                        """,
                        new { Id = recordId },
                        cancellationToken: CancellationToken.None));
            }

            if (branch == 9)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.FindingProperties (FindingRecordId, PropertySortOrder, PropertyKey, PropertyValue)
                        VALUES (@Id, 0, N'k', N'v');
                        """,
                        new { Id = recordId },
                        cancellationToken: CancellationToken.None));
            }

            if (branch == 10)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.FindingTraceGraphNodesExamined (FindingRecordId, SortOrder, NodeId)
                        VALUES (@Id, 0, N'tn');
                        """,
                        new { Id = recordId },
                        cancellationToken: CancellationToken.None));
            }

            if (branch == 11)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.FindingTraceRulesApplied (FindingRecordId, SortOrder, RuleText)
                        VALUES (@Id, 0, N'rule');
                        """,
                        new { Id = recordId },
                        cancellationToken: CancellationToken.None));
            }

            if (branch == 12)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.FindingTraceDecisionsTaken (FindingRecordId, SortOrder, DecisionText)
                        VALUES (@Id, 0, N'dec');
                        """,
                        new { Id = recordId },
                        cancellationToken: CancellationToken.None));
            }

            if (branch == 13)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.FindingTraceAlternativePaths (FindingRecordId, SortOrder, PathText)
                        VALUES (@Id, 0, N'path');
                        """,
                        new { Id = recordId },
                        cancellationToken: CancellationToken.None));
            }

            if (branch == 14)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.FindingTraceNotes (FindingRecordId, SortOrder, NoteText)
                        VALUES (@Id, 0, N'note');
                        """,
                        new { Id = recordId },
                        cancellationToken: CancellationToken.None));
            }

            FindingsSnapshotStorageRow row2 = await RowAsync(connection, findingsId);
            FindingsSnapshot snap2 =
                await FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync(connection, row2,
                    CancellationToken.None);
            Finding f = snap2.Findings.Should().ContainSingle().Subject;

            if (branch >= 2 && branch <= 6)
            {
                FindingSeverity expected = branch switch
                {
                    2 => FindingSeverity.Info,
                    3 => FindingSeverity.Warning,
                    4 => FindingSeverity.Error,
                    5 => FindingSeverity.Critical,
                    6 => FindingSeverity.Info,
                    _ => FindingSeverity.Info
                };

                f.Severity.Should().Be(expected);
                return;
            }

            if (branch == 7)
            {
                f.RelatedNodeIds.Should().Equal("n1");
                return;
            }

            if (branch == 8)
            {
                f.RecommendedActions.Should().Equal("act");
                return;
            }

            if (branch == 9)
            {
                f.Properties.Should().ContainKey("k").WhoseValue.Should().Be("v");
                return;
            }

            if (branch == 10)
            {
                f.Trace.GraphNodeIdsExamined.Should().Equal("tn");
                return;
            }

            if (branch == 11)
            {
                f.Trace.RulesApplied.Should().Equal("rule");
                return;
            }

            if (branch == 12)
            {
                f.Trace.DecisionsTaken.Should().Equal("dec");
                return;
            }

            if (branch == 13)
            {
                f.Trace.AlternativePathsConsidered.Should().Equal("path");
                return;
            }

            if (branch == 14)
            {
                f.Trace.Notes.Should().Equal("note");
            }
        }
    }

    private static async Task<FindingsSnapshotStorageRow> RowAsync(SqlConnection connection, Guid findingsId)
    {
        return await connection.QuerySingleAsync<FindingsSnapshotStorageRow>(
            new CommandDefinition(
                """
                SELECT FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc, SchemaVersion, FindingsJson
                FROM dbo.FindingsSnapshots WHERE FindingsSnapshotId = @Id;
                """,
                new { Id = findingsId },
                cancellationToken: CancellationToken.None));
    }
}
