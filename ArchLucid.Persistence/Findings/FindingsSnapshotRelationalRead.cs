using ArchLucid.Decisioning.Models;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Findings;

/// <summary>Builds <see cref="FindingsSnapshot"/> from relational finding tables when rows exist; otherwise <c>FindingsJson</c>.</summary>
internal static class FindingsSnapshotRelationalRead
{
    internal static async Task<FindingsSnapshot> LoadRelationalSnapshotAsync(
        SqlConnection connection,
        FindingsSnapshotStorageRow row,
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
            List<Finding> legacyFindings = FindingsSnapshotLegacyJsonReader.DeserializeFindings(row.FindingsJson);

            return new FindingsSnapshot
            {
                FindingsSnapshotId = row.FindingsSnapshotId,
                RunId = row.RunId,
                ContextSnapshotId = row.ContextSnapshotId,
                GraphSnapshotId = row.GraphSnapshotId,
                CreatedUtc = row.CreatedUtc,
                SchemaVersion = row.SchemaVersion,
                Findings = legacyFindings,
            };
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

        Dictionary<Guid, Dictionary<string, string>> propsByRecord = await LoadPropertiesAsync(connection, recordIds, ct);

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

    private sealed class FindingRecordRow
    {
        public Guid FindingRecordId { get; init; }
        public int SortOrder { get; init; }
        public string FindingId { get; init; } = null!;
        public int FindingSchemaVersion { get; init; }
        public string FindingType { get; init; } = null!;
        public string Category { get; init; } = null!;
        public string EngineType { get; init; } = null!;
        public string Severity { get; init; } = null!;
        public string Title { get; init; } = null!;
        public string Rationale { get; init; } = null!;
        public string? PayloadType { get; init; }
        public string? PayloadJson { get; init; }
    }

    private sealed class FindingChildStringRow
    {
        public Guid FindingRecordId { get; init; }
        public int SortOrder { get; init; }
        public string Item { get; init; } = null!;
    }

    private sealed class FindingPropertyRow
    {
        public Guid FindingRecordId { get; init; }
        public int PropertySortOrder { get; init; }
        public string PropertyKey { get; init; } = null!;
        public string PropertyValue { get; init; } = null!;
    }
}
