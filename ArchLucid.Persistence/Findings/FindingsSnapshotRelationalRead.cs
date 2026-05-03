using ArchLucid.Contracts.Findings;
using ArchLucid.Decisioning.Models;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Findings;

/// <summary>
///     Builds <see cref="FindingsSnapshot" /> from relational finding tables when rows exist; otherwise
///     <c>FindingsJson</c>.
/// </summary>
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
                                      Severity, Title, Rationale, PayloadType, PayloadJson,
                                      RequestInputRef, RunIdRef, AgentExecutionTraceId,
                                      ModelDeploymentName, ModelVersion, PromptTemplateId, PromptTemplateVersion,
                                      ConfidenceScore, EvaluationConfidenceScore, EvaluationConfidenceLevel, PolicyRuleId,
                                      HumanReviewStatus, ReviewedByUserId, ReviewedAtUtc, ReviewNotes
                                  FROM dbo.FindingRecords
                                  WHERE FindingsSnapshotId = @FindingsSnapshotId
                                  ORDER BY SortOrder;
                                  """;

        List<FindingRecordRow> records = (await connection.QueryAsync<FindingRecordRow>(
            new CommandDefinition(
                recordsSql,
                new { row.FindingsSnapshotId },
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
                GenerationStatus = FindingsSnapshotGenerationStatusParser.Parse(row.GenerationStatus),
                Findings = legacyFindings
            };
        }

        List<Guid> recordIds = records.Select(r => r.FindingRecordId).ToList();

        ChildRelationalSlices slices = await LoadChildRelationalSlicesAsync(connection, recordIds, ct);

        Dictionary<Guid, List<string>> relatedByRecord = slices.RelatedNodes;

        Dictionary<Guid, List<string>> actionsByRecord = slices.RecommendedActions;

        Dictionary<Guid, Dictionary<string, string>> propsByRecord = slices.Properties;

        Dictionary<Guid, List<string>> traceNodesByRecord = slices.TraceGraphNodesExamined;

        Dictionary<Guid, List<string>> traceRulesByRecord = slices.TraceRulesApplied;

        Dictionary<Guid, List<string>> traceDecisionsByRecord = slices.TraceDecisionsTaken;

        Dictionary<Guid, List<string>> tracePathsByRecord = slices.TraceAlternativePaths;

        Dictionary<Guid, List<string>> traceNotesByRecord = slices.TraceNotes;
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
                Severity = Enum.Parse<FindingSeverity>(rec.Severity, true),
                Title = rec.Title,
                Rationale = rec.Rationale,
                PayloadType = rec.PayloadType,
                Payload = FindingPayloadJsonCodec.DeserializePayload(rec.PayloadJson, rec.PayloadType),
                RelatedNodeIds = relatedByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                RecommendedActions = actionsByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                Properties =
                    propsByRecord.GetValueOrDefault(rec.FindingRecordId) ??
                    new Dictionary<string, string>(StringComparer.Ordinal),
                RequestInputRef = rec.RequestInputRef,
                RunIdRef = rec.RunIdRef,
                AgentExecutionTraceId = rec.AgentExecutionTraceId,
                ModelDeploymentName = rec.ModelDeploymentName,
                ModelVersion = rec.ModelVersion,
                PromptTemplateId = rec.PromptTemplateId,
                PromptTemplateVersion = rec.PromptTemplateVersion,
                ConfidenceScore = rec.ConfidenceScore,
                EvaluationConfidenceScore = rec.EvaluationConfidenceScore,
                ConfidenceLevel = ParseEvaluationConfidenceLevel(rec.EvaluationConfidenceLevel),
                PolicyRuleId = rec.PolicyRuleId,
                HumanReviewStatus = ParseHumanReviewStatus(rec.HumanReviewStatus),
                ReviewedByUserId = rec.ReviewedByUserId,
                ReviewedAtUtc = rec.ReviewedAtUtc is { } ra ? new DateTimeOffset(DateTime.SpecifyKind(ra, DateTimeKind.Utc)) : null,
                ReviewNotes = rec.ReviewNotes,
                Trace = new ExplainabilityTrace
                {
                    SourceAgentExecutionTraceId = rec.AgentExecutionTraceId,
                    GraphNodeIdsExamined = traceNodesByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                    RulesApplied = traceRulesByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                    DecisionsTaken = traceDecisionsByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                    AlternativePathsConsidered =
                        tracePathsByRecord.GetValueOrDefault(rec.FindingRecordId) ?? [],
                    Notes = traceNotesByRecord.GetValueOrDefault(rec.FindingRecordId) ?? []
                }
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
            GenerationStatus = FindingsSnapshotGenerationStatusParser.Parse(row.GenerationStatus),
            Findings = findings
        };
    }

    private static FindingConfidenceLevel? ParseEvaluationConfidenceLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return Enum.TryParse(raw.Trim(), ignoreCase: true, out FindingConfidenceLevel lvl) ? lvl : null;
    }

    private static FindingHumanReviewStatus ParseHumanReviewStatus(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), true, out FindingHumanReviewStatus st))
            return st;

        return FindingHumanReviewStatus.NotRequired;
    }


    private sealed record ChildRelationalSlices(
        Dictionary<Guid, List<string>> RelatedNodes,
        Dictionary<Guid, List<string>> RecommendedActions,
        Dictionary<Guid, Dictionary<string, string>> Properties,
        Dictionary<Guid, List<string>> TraceGraphNodesExamined,
        Dictionary<Guid, List<string>> TraceRulesApplied,
        Dictionary<Guid, List<string>> TraceDecisionsTaken,
        Dictionary<Guid, List<string>> TraceAlternativePaths,
        Dictionary<Guid, List<string>> TraceNotes);

    private static async Task<ChildRelationalSlices> LoadChildRelationalSlicesAsync(
        SqlConnection connection,
        List<Guid> recordIds,
        CancellationToken ct)
    {
        if (recordIds.Count == 0)
            return new ChildRelationalSlices(
                new Dictionary<Guid, List<string>>(),
                new Dictionary<Guid, List<string>>(),
                new Dictionary<Guid, Dictionary<string, string>>(),
                new Dictionary<Guid, List<string>>(),
                new Dictionary<Guid, List<string>>(),
                new Dictionary<Guid, List<string>>(),
                new Dictionary<Guid, List<string>>(),
                new Dictionary<Guid, List<string>>());

        const string batchedSql = """
                                   SELECT FindingRecordId, SortOrder, NodeId AS Item
                                   FROM dbo.FindingRelatedNodes
                                   WHERE FindingRecordId IN @Ids
                                   ORDER BY FindingRecordId, SortOrder;

                                   SELECT FindingRecordId, SortOrder, ActionText AS Item
                                   FROM dbo.FindingRecommendedActions
                                   WHERE FindingRecordId IN @Ids
                                   ORDER BY FindingRecordId, SortOrder;

                                   SELECT FindingRecordId, PropertySortOrder, PropertyKey, PropertyValue
                                   FROM dbo.FindingProperties
                                   WHERE FindingRecordId IN @Ids
                                   ORDER BY FindingRecordId, PropertySortOrder;

                                   SELECT FindingRecordId, SortOrder, NodeId AS Item
                                   FROM dbo.FindingTraceGraphNodesExamined
                                   WHERE FindingRecordId IN @Ids
                                   ORDER BY FindingRecordId, SortOrder;

                                   SELECT FindingRecordId, SortOrder, RuleText AS Item
                                   FROM dbo.FindingTraceRulesApplied
                                   WHERE FindingRecordId IN @Ids
                                   ORDER BY FindingRecordId, SortOrder;

                                   SELECT FindingRecordId, SortOrder, DecisionText AS Item
                                   FROM dbo.FindingTraceDecisionsTaken
                                   WHERE FindingRecordId IN @Ids
                                   ORDER BY FindingRecordId, SortOrder;

                                   SELECT FindingRecordId, SortOrder, PathText AS Item
                                   FROM dbo.FindingTraceAlternativePaths
                                   WHERE FindingRecordId IN @Ids
                                   ORDER BY FindingRecordId, SortOrder;

                                   SELECT FindingRecordId, SortOrder, NoteText AS Item
                                   FROM dbo.FindingTraceNotes
                                   WHERE FindingRecordId IN @Ids
                                   ORDER BY FindingRecordId, SortOrder;
                                   """;

        await using SqlMapper.GridReader reader = await connection.QueryMultipleAsync(
            new CommandDefinition(batchedSql, new { Ids = recordIds }, cancellationToken: ct));

        Dictionary<Guid, List<string>> related =
            FoldFindingChildStrings(reader.Read<FindingChildStringRow>().ToList());

        Dictionary<Guid, List<string>> actions =
            FoldFindingChildStrings(reader.Read<FindingChildStringRow>().ToList());

        Dictionary<Guid, Dictionary<string, string>> props =
            FoldFindingProperties(reader.Read<FindingPropertyRow>().ToList());

        Dictionary<Guid, List<string>> traceNodes =
            FoldFindingChildStrings(reader.Read<FindingChildStringRow>().ToList());

        Dictionary<Guid, List<string>> traceRules =
            FoldFindingChildStrings(reader.Read<FindingChildStringRow>().ToList());

        Dictionary<Guid, List<string>> traceDecisions =
            FoldFindingChildStrings(reader.Read<FindingChildStringRow>().ToList());

        Dictionary<Guid, List<string>> tracePaths =
            FoldFindingChildStrings(reader.Read<FindingChildStringRow>().ToList());

        Dictionary<Guid, List<string>> traceNotes =
            FoldFindingChildStrings(reader.Read<FindingChildStringRow>().ToList());

        return new ChildRelationalSlices(
            related,
            actions,
            props,
            traceNodes,
            traceRules,
            traceDecisions,
            tracePaths,
            traceNotes);
    }

    private static Dictionary<Guid, List<string>> FoldFindingChildStrings(IReadOnlyList<FindingChildStringRow> rows)
    {
        Dictionary<Guid, List<string>> result = new();

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

    private static Dictionary<Guid, Dictionary<string, string>> FoldFindingProperties(
        IReadOnlyList<FindingPropertyRow> rows)
    {
        Dictionary<Guid, Dictionary<string, string>> result = new();

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
        public Guid FindingRecordId
        {
            get;
            init;
        }

        public int SortOrder
        {
            get;
            init;
        }

        public string FindingId
        {
            get;
            init;
        } = null!;

        public int FindingSchemaVersion
        {
            get;
            init;
        }

        public string FindingType
        {
            get;
            init;
        } = null!;

        public string Category
        {
            get;
            init;
        } = null!;

        public string EngineType
        {
            get;
            init;
        } = null!;

        public string Severity
        {
            get;
            init;
        } = null!;

        public string Title
        {
            get;
            init;
        } = null!;

        public string Rationale
        {
            get;
            init;
        } = null!;

        public string? PayloadType
        {
            get;
            init;
        }

        public string? PayloadJson
        {
            get;
            init;
        }

        public string? RequestInputRef
        {
            get;
            init;
        }

        public string? RunIdRef
        {
            get;
            init;
        }

        public string? AgentExecutionTraceId
        {
            get;
            init;
        }

        public string? ModelDeploymentName
        {
            get;
            init;
        }

        public string? ModelVersion
        {
            get;
            init;
        }

        public string? PromptTemplateId
        {
            get;
            init;
        }

        public string? PromptTemplateVersion
        {
            get;
            init;
        }

        public double? ConfidenceScore
        {
            get;
            init;
        }

        public int? EvaluationConfidenceScore
        {
            get;
            init;
        }

        public string? EvaluationConfidenceLevel
        {
            get;
            init;
        }

        public string? PolicyRuleId
        {
            get;
            init;
        }

        public string? HumanReviewStatus
        {
            get;
            init;
        }

        public string? ReviewedByUserId
        {
            get;
            init;
        }

        public DateTime? ReviewedAtUtc
        {
            get;
            init;
        }

        public string? ReviewNotes
        {
            get;
            init;
        }
    }

    private sealed class FindingChildStringRow
    {
        public Guid FindingRecordId
        {
            get;
            init;
        }

        public int SortOrder
        {
            get;
            init;
        }

        public string Item
        {
            get;
            init;
        } = null!;
    }

    private sealed class FindingPropertyRow
    {
        public Guid FindingRecordId
        {
            get;
            init;
        }

        public int PropertySortOrder
        {
            get;
            init;
        }

        public string PropertyKey
        {
            get;
            init;
        } = null!;

        public string PropertyValue
        {
            get;
            init;
        } = null!;
    }
}
