using System.Data;

using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Findings;
using ArchiForge.Persistence.RelationalRead;
using ArchiForge.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// SQL Server-backed <see cref="IFindingsSnapshotRepository"/> with dual-write to <c>FindingsJson</c> and relational
/// finding tables; reads prefer <c>dbo.FindingRecords</c> when any exist, otherwise deserialize <c>FindingsJson</c>.
/// Typed <see cref="Finding.Payload"/> is stored only in <c>FindingRecords.PayloadJson</c> (sidecar). All other finding
/// fields and trace lists are relational with stable <c>SortOrder</c>. <see cref="FindingsSnapshotMigrator"/> runs on
/// save and after load so schema versioning stays consistent.
/// </summary>
public sealed class SqlFindingsSnapshotRepository(ISqlConnectionFactory connectionFactory) : IFindingsSnapshotRepository
{
    public async Task SaveAsync(
        FindingsSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (connection is not null)
        {
            await SaveCoreAsync(snapshot, connection, transaction, ct);
            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        await using SqlTransaction tx = owned.BeginTransaction();

        try
        {
            await SaveCoreAsync(snapshot, owned, tx, ct);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static async Task SaveCoreAsync(
        FindingsSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        FindingsSnapshotMigrator.Apply(snapshot);

        const string headerSql = """
            INSERT INTO dbo.FindingsSnapshots
            (
                FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                SchemaVersion, FindingsJson
            )
            VALUES
            (
                @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                @SchemaVersion, @FindingsJson
            );
            """;

        object headerArgs = new
        {
            snapshot.FindingsSnapshotId,
            snapshot.RunId,
            snapshot.ContextSnapshotId,
            snapshot.GraphSnapshotId,
            snapshot.CreatedUtc,
            snapshot.SchemaVersion,
            FindingsJson = JsonEntitySerializer.Serialize(snapshot),
        };

        await connection.ExecuteAsync(new CommandDefinition(headerSql, headerArgs, transaction, cancellationToken: ct))
            ;

        await InsertFindingsRelationalFromSnapshotAsync(snapshot, connection, transaction, ct);
    }

    internal static async Task InsertFindingsRelationalFromSnapshotAsync(
        FindingsSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        for (int i = 0; i < snapshot.Findings.Count; i++)
        {
            Finding finding = snapshot.Findings[i];
            Guid recordId = Guid.NewGuid();

            await InsertFindingRecordAsync(
                connection,
                transaction,
                snapshot.FindingsSnapshotId,
                recordId,
                sortOrder: i,
                finding,
                ct);

            await InsertFindingChildrenAsync(connection, transaction, recordId, finding, ct);
        }
    }

    private static async Task InsertFindingRecordAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid findingsSnapshotId,
        Guid findingRecordId,
        int sortOrder,
        Finding finding,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.FindingRecords
            (
                FindingRecordId, FindingsSnapshotId, SortOrder,
                FindingId, FindingSchemaVersion, FindingType, Category, EngineType,
                Severity, Title, Rationale, PayloadType, PayloadJson
            )
            VALUES
            (
                @FindingRecordId, @FindingsSnapshotId, @SortOrder,
                @FindingId, @FindingSchemaVersion, @FindingType, @Category, @EngineType,
                @Severity, @Title, @Rationale, @PayloadType, @PayloadJson
            );
            """;

        object args = new
        {
            FindingRecordId = findingRecordId,
            FindingsSnapshotId = findingsSnapshotId,
            SortOrder = sortOrder,
            finding.FindingId,
            finding.FindingSchemaVersion,
            finding.FindingType,
            finding.Category,
            finding.EngineType,
            Severity = finding.Severity.ToString(),
            finding.Title,
            finding.Rationale,
            finding.PayloadType,
            PayloadJson = FindingPayloadJsonCodec.SerializePayload(finding.Payload),
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));
    }

    private static async Task InsertFindingChildrenAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid findingRecordId,
        Finding finding,
        CancellationToken ct)
    {
        const string insertRelatedSql = """
            INSERT INTO dbo.FindingRelatedNodes (FindingRecordId, SortOrder, NodeId)
            VALUES (@FindingRecordId, @SortOrder, @NodeId);
            """;

        for (int r = 0; r < finding.RelatedNodeIds.Count; r++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertRelatedSql,
                    new
                    {
                        FindingRecordId = findingRecordId,
                        SortOrder = r,
                        NodeId = finding.RelatedNodeIds[r],
                    },
                    transaction,
                    cancellationToken: ct));
        }

        const string insertActionSql = """
            INSERT INTO dbo.FindingRecommendedActions (FindingRecordId, SortOrder, ActionText)
            VALUES (@FindingRecordId, @SortOrder, @ActionText);
            """;

        for (int a = 0; a < finding.RecommendedActions.Count; a++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertActionSql,
                    new
                    {
                        FindingRecordId = findingRecordId,
                        SortOrder = a,
                        ActionText = finding.RecommendedActions[a],
                    },
                    transaction,
                    cancellationToken: ct));
        }

        const string insertPropSql = """
            INSERT INTO dbo.FindingProperties (FindingRecordId, PropertySortOrder, PropertyKey, PropertyValue)
            VALUES (@FindingRecordId, @PropertySortOrder, @PropertyKey, @PropertyValue);
            """;

        List<KeyValuePair<string, string>> orderedProps = finding.Properties
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        for (int p = 0; p < orderedProps.Count; p++)
        {
            KeyValuePair<string, string> kv = orderedProps[p];

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertPropSql,
                    new
                    {
                        FindingRecordId = findingRecordId,
                        PropertySortOrder = p,
                        PropertyKey = kv.Key,
                        PropertyValue = kv.Value,
                    },
                    transaction,
                    cancellationToken: ct));
        }

        await InsertOrderedStringRowsAsync(
            connection,
            transaction,
            findingRecordId,
            """
            INSERT INTO dbo.FindingTraceGraphNodesExamined (FindingRecordId, SortOrder, NodeId)
            VALUES (@FindingRecordId, @SortOrder, @Text);
            """,
            finding.Trace.GraphNodeIdsExamined,
            ct);

        await InsertOrderedStringRowsAsync(
            connection,
            transaction,
            findingRecordId,
            """
            INSERT INTO dbo.FindingTraceRulesApplied (FindingRecordId, SortOrder, RuleText)
            VALUES (@FindingRecordId, @SortOrder, @Text);
            """,
            finding.Trace.RulesApplied,
            ct);

        await InsertOrderedStringRowsAsync(
            connection,
            transaction,
            findingRecordId,
            """
            INSERT INTO dbo.FindingTraceDecisionsTaken (FindingRecordId, SortOrder, DecisionText)
            VALUES (@FindingRecordId, @SortOrder, @Text);
            """,
            finding.Trace.DecisionsTaken,
            ct);

        await InsertOrderedStringRowsAsync(
            connection,
            transaction,
            findingRecordId,
            """
            INSERT INTO dbo.FindingTraceAlternativePaths (FindingRecordId, SortOrder, PathText)
            VALUES (@FindingRecordId, @SortOrder, @Text);
            """,
            finding.Trace.AlternativePathsConsidered,
            ct);

        await InsertOrderedStringRowsAsync(
            connection,
            transaction,
            findingRecordId,
            """
            INSERT INTO dbo.FindingTraceNotes (FindingRecordId, SortOrder, NoteText)
            VALUES (@FindingRecordId, @SortOrder, @Text);
            """,
            finding.Trace.Notes,
            ct);
    }

    private static async Task InsertOrderedStringRowsAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid findingRecordId,
        string sql,
        List<string> items,
        CancellationToken ct)
    {
        for (int i = 0; i < items.Count; i++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        FindingRecordId = findingRecordId,
                        SortOrder = i,
                        Text = items[i],
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    public async Task<FindingsSnapshot?> GetByIdAsync(Guid findingsSnapshotId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                SchemaVersion, FindingsJson
            FROM dbo.FindingsSnapshots
            WHERE FindingsSnapshotId = @FindingsSnapshotId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        FindingsSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<FindingsSnapshotStorageRow>(
            new CommandDefinition(
                sql,
                new
                {
                    FindingsSnapshotId = findingsSnapshotId,
                },
                cancellationToken: ct));

        if (row is null)
            return null;

        int recordCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction: null,
            "SELECT COUNT(1) FROM dbo.FindingRecords WHERE FindingsSnapshotId = @FindingsSnapshotId",
            new
            {
                FindingsSnapshotId = findingsSnapshotId,
            },
            ct);

        if (recordCount == 0)
            return FindingsSnapshotJsonFallback.FromHeaderRow(row);

        FindingsSnapshot snapshot = await FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync(connection, row, ct);
        FindingsSnapshotMigrator.Apply(snapshot);
        return snapshot;
    }

    /// <summary>
    /// Inserts relational finding rows when <c>FindingRecords</c> is still empty (idempotent).
    /// </summary>
    internal static async Task BackfillRelationalSlicesAsync(
        FindingsSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(connection);

        int recordCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.FindingRecords WHERE FindingsSnapshotId = @FindingsSnapshotId",
            new
            {
                snapshot.FindingsSnapshotId,
            },
            ct);

        if (recordCount > 0 || snapshot.Findings.Count == 0)
            return;

        FindingsSnapshotMigrator.Apply(snapshot);
        await InsertFindingsRelationalFromSnapshotAsync(snapshot, connection, transaction, ct);
    }
}
