using System.Data;

using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Findings;
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
        using SqlTransaction tx = owned.BeginTransaction();

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
            SchemaVersion = snapshot.SchemaVersion,
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
        FindingsSnapshotHeaderRow? row = await connection.QuerySingleOrDefaultAsync<FindingsSnapshotHeaderRow>(
            new CommandDefinition(
                sql,
                new
                {
                    FindingsSnapshotId = findingsSnapshotId,
                },
                cancellationToken: ct));

        if (row is null)
            return null;

        int recordCount = await ScalarCountAsync(
            connection,
            transaction: null,
            "SELECT COUNT(1) FROM dbo.FindingRecords WHERE FindingsSnapshotId = @FindingsSnapshotId",
            new
            {
                FindingsSnapshotId = findingsSnapshotId,
            },
            ct);

        if (recordCount == 0)
            return DeserializeFromFindingsJson(row);

        FindingsSnapshot snapshot = await LoadRelationalSnapshotAsync(connection, row, ct);
        FindingsSnapshotMigrator.Apply(snapshot);
        return snapshot;
    }

    private static FindingsSnapshot DeserializeFromFindingsJson(FindingsSnapshotHeaderRow row)
    {
        FindingsSnapshot snapshot = JsonEntitySerializer.Deserialize<FindingsSnapshot>(row.FindingsJson);
        snapshot.FindingsSnapshotId = row.FindingsSnapshotId;
        snapshot.RunId = row.RunId;
        snapshot.ContextSnapshotId = row.ContextSnapshotId;
        snapshot.GraphSnapshotId = row.GraphSnapshotId;
        snapshot.CreatedUtc = row.CreatedUtc;
        snapshot.SchemaVersion = row.SchemaVersion;
        FindingsSnapshotMigrator.Apply(snapshot);
        return snapshot;
    }

    private static async Task<FindingsSnapshot> LoadRelationalSnapshotAsync(
        SqlConnection connection,
        FindingsSnapshotHeaderRow row,
        CancellationToken ct)
    {
        const string recordsSql = """
            SELECT
                FindingRecordId, SortOrder, FindingId, FindingSchemaVersion, FindingType, Category, EngineType,
                Severity, Title, Rationale, PayloadType, PayloadJson
            FROM dbo.FindingRecords
            WHERE FindingsSnapshotId = @FindingsSnapshotId
            ORDER BY SortOrder;
            """;

        List<FindingRecordRow> records = (await connection.QueryAsync<FindingRecordRow>(
            new CommandDefinition(
                recordsSql,
                new
                {
                    row.FindingsSnapshotId,
                },
                cancellationToken: ct))).ToList();

        if (records.Count == 0)
        {
            return DeserializeFromFindingsJson(row);
        }

        List<Guid> recordIds = records.Select(r => r.FindingRecordId).ToList();

        Dictionary<Guid, List<string>> relatedByRecord = await LoadOrderedPairsAsync(
            connection,
            """
            SELECT FindingRecordId, SortOrder, NodeId AS Item
            FROM dbo.FindingRelatedNodes
            WHERE FindingRecordId IN @Ids
            ORDER BY FindingRecordId, SortOrder;
            """,
            recordIds,
            ct);

        Dictionary<Guid, List<string>> actionsByRecord = await LoadOrderedPairsAsync(
            connection,
            """
            SELECT FindingRecordId, SortOrder, ActionText AS Item
            FROM dbo.FindingRecommendedActions
            WHERE FindingRecordId IN @Ids
            ORDER BY FindingRecordId, SortOrder;
            """,
            recordIds,
            ct);

        Dictionary<Guid, Dictionary<string, string>> propsByRecord = await LoadPropertiesAsync(connection, recordIds, ct)
            ;

        Dictionary<Guid, List<string>> traceNodesByRecord = await LoadOrderedPairsAsync(
            connection,
            """
            SELECT FindingRecordId, SortOrder, NodeId AS Item
            FROM dbo.FindingTraceGraphNodesExamined
            WHERE FindingRecordId IN @Ids
            ORDER BY FindingRecordId, SortOrder;
            """,
            recordIds,
            ct);

        Dictionary<Guid, List<string>> traceRulesByRecord = await LoadOrderedPairsAsync(
            connection,
            """
            SELECT FindingRecordId, SortOrder, RuleText AS Item
            FROM dbo.FindingTraceRulesApplied
            WHERE FindingRecordId IN @Ids
            ORDER BY FindingRecordId, SortOrder;
            """,
            recordIds,
            ct);

        Dictionary<Guid, List<string>> traceDecisionsByRecord = await LoadOrderedPairsAsync(
            connection,
            """
            SELECT FindingRecordId, SortOrder, DecisionText AS Item
            FROM dbo.FindingTraceDecisionsTaken
            WHERE FindingRecordId IN @Ids
            ORDER BY FindingRecordId, SortOrder;
            """,
            recordIds,
            ct);

        Dictionary<Guid, List<string>> tracePathsByRecord = await LoadOrderedPairsAsync(
            connection,
            """
            SELECT FindingRecordId, SortOrder, PathText AS Item
            FROM dbo.FindingTraceAlternativePaths
            WHERE FindingRecordId IN @Ids
            ORDER BY FindingRecordId, SortOrder;
            """,
            recordIds,
            ct);

        Dictionary<Guid, List<string>> traceNotesByRecord = await LoadOrderedPairsAsync(
            connection,
            """
            SELECT FindingRecordId, SortOrder, NoteText AS Item
            FROM dbo.FindingTraceNotes
            WHERE FindingRecordId IN @Ids
            ORDER BY FindingRecordId, SortOrder;
            """,
            recordIds,
            ct);

        List<Finding> findings = [];
        foreach (FindingRecordRow rec in records)
        {
            Finding finding = new()
            {
                FindingId = rec.FindingId,
                FindingSchemaVersion = rec.FindingSchemaVersion,
                FindingType = rec.FindingType,
                Category = rec.Category,
                EngineType = rec.EngineType,
                Severity = Enum.Parse<FindingSeverity>(rec.Severity, ignoreCase: true),
                Title = rec.Title,
                Rationale = rec.Rationale,
                PayloadType = rec.PayloadType,
                Payload = FindingPayloadJsonCodec.DeserializePayload(rec.PayloadJson, rec.PayloadType),
                RelatedNodeIds = relatedByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                RecommendedActions = actionsByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                Properties = propsByRecord.GetValueOrDefault(rec.FindingRecordId) ?? new Dictionary<string, string>(StringComparer.Ordinal),
                Trace = new ExplainabilityTrace
                {
                    GraphNodeIdsExamined = traceNodesByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                    RulesApplied = traceRulesByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                    DecisionsTaken = traceDecisionsByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                    AlternativePathsConsidered = tracePathsByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                    Notes = traceNotesByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                },
            };

            findings.Add(finding);
        }

        return new FindingsSnapshot
        {
            FindingsSnapshotId = row.FindingsSnapshotId,
            RunId = row.RunId,
            ContextSnapshotId = row.ContextSnapshotId,
            GraphSnapshotId = row.GraphSnapshotId,
            CreatedUtc = row.CreatedUtc,
            SchemaVersion = row.SchemaVersion,
            Findings = findings,
        };
    }

    private static async Task<Dictionary<Guid, List<string>>> LoadOrderedPairsAsync(
        SqlConnection connection,
        string sql,
        List<Guid> recordIds,
        CancellationToken ct)
    {
        Dictionary<Guid, List<string>> result = new();

        if (recordIds.Count == 0)
            return result;

        IEnumerable<FindingChildStringRow> rows = await connection.QueryAsync<FindingChildStringRow>(
            new CommandDefinition(
                sql,
                new
                {
                    Ids = recordIds,
                },
                cancellationToken: ct));

        foreach (FindingChildStringRow row in rows)
        {
            if (!result.TryGetValue(row.FindingRecordId, out List<string>? list))
            {
                list = [];
                result[row.FindingRecordId] = list;
            }

            list.Add(row.Item);
        }

        return result;
    }

    private static async Task<Dictionary<Guid, Dictionary<string, string>>> LoadPropertiesAsync(
        SqlConnection connection,
        List<Guid> recordIds,
        CancellationToken ct)
    {
        Dictionary<Guid, Dictionary<string, string>> result = new();

        if (recordIds.Count == 0)
            return result;

        const string sql = """
            SELECT FindingRecordId, PropertySortOrder, PropertyKey, PropertyValue
            FROM dbo.FindingProperties
            WHERE FindingRecordId IN @Ids
            ORDER BY FindingRecordId, PropertySortOrder;
            """;

        IEnumerable<FindingPropertyRow> rows = await connection.QueryAsync<FindingPropertyRow>(
            new CommandDefinition(
                sql,
                new
                {
                    Ids = recordIds,
                },
                cancellationToken: ct));

        foreach (FindingPropertyRow row in rows)
        {
            if (!result.TryGetValue(row.FindingRecordId, out Dictionary<string, string>? dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                result[row.FindingRecordId] = dict;
            }

            dict[row.PropertyKey] = row.PropertyValue;
        }

        return result;
    }

    private static async Task<int> ScalarCountAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string sql,
        object param,
        CancellationToken ct)
    {
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, transaction, cancellationToken: ct))
            ;
        return count;
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

        int recordCount = await ScalarCountAsync(
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

    private sealed class FindingsSnapshotHeaderRow
    {
        public Guid FindingsSnapshotId
        {
            get; init;
        }

        public Guid RunId
        {
            get; init;
        }

        public Guid ContextSnapshotId
        {
            get; init;
        }

        public Guid GraphSnapshotId
        {
            get; init;
        }

        public DateTime CreatedUtc
        {
            get; init;
        }

        public int SchemaVersion
        {
            get; init;
        }

        public string FindingsJson { get; init; } = null!;
    }

    private sealed class FindingRecordRow
    {
        public Guid FindingRecordId
        {
            get; init;
        }

        public int SortOrder
        {
            get; init;
        }

        public string FindingId { get; init; } = null!;

        public int FindingSchemaVersion
        {
            get; init;
        }

        public string FindingType { get; init; } = null!;

        public string Category { get; init; } = null!;

        public string EngineType { get; init; } = null!;

        public string Severity { get; init; } = null!;

        public string Title { get; init; } = null!;

        public string Rationale { get; init; } = null!;

        public string? PayloadType
        {
            get; init;
        }

        public string? PayloadJson
        {
            get; init;
        }
    }

    private sealed class FindingChildStringRow
    {
        public Guid FindingRecordId
        {
            get; init;
        }

        public int SortOrder
        {
            get; init;
        }

        public string Item { get; init; } = null!;
    }

    private sealed class FindingPropertyRow
    {
        public Guid FindingRecordId
        {
            get; init;
        }

        public int PropertySortOrder
        {
            get; init;
        }

        public string PropertyKey { get; init; } = null!;

        public string PropertyValue { get; init; } = null!;
    }
}
