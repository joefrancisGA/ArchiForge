using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Interfaces;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Findings;

/// <summary>
///     Dapper read joining <c>dbo.FindingRecords</c>, snapshots, runs, optional <c>dbo.DecisioningTraces</c>, and audit.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; covered via API integration tests.")]
public sealed class DapperFindingInspectReadRepository(ISqlConnectionFactory connectionFactory)
    : IFindingInspectReadRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<FindingInspectResponse?> GetInspectAsync(ScopeContext scope, string findingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (string.IsNullOrWhiteSpace(findingId))
            throw new ArgumentException("Finding id is required.", nameof(findingId));

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT TOP 1
                               fr.FindingId,
                               fr.PayloadJson,
                               fr.ModelDeploymentName,
                               fr.PromptTemplateVersion,
                               fr.ConfidenceScore,
                               fr.EvaluationConfidenceScore,
                               fr.EvaluationConfidenceLevel,
                               fr.HumanReviewStatus,
                               r.RunId,
                               r.CurrentManifestVersion,
                               r.GoldenManifestId,
                               dt.AppliedRuleIdsJson
                           FROM dbo.FindingRecords fr
                           INNER JOIN dbo.FindingsSnapshots fs ON fs.FindingsSnapshotId = fr.FindingsSnapshotId
                           INNER JOIN dbo.Runs r ON r.RunId = fs.RunId
                           LEFT JOIN dbo.DecisioningTraces dt
                               ON dt.DecisionTraceId = r.DecisionTraceId
                              AND dt.TenantId = r.TenantId
                              AND dt.WorkspaceId = r.WorkspaceId
                              AND dt.ProjectId = r.ScopeProjectId
                           WHERE fr.FindingId = @FindingId
                             AND r.TenantId = @TenantId
                             AND r.WorkspaceId = @WorkspaceId
                             AND r.ScopeProjectId = @ScopeProjectId
                             AND (r.ArchivedUtc IS NULL);
                           """;

        MainRow? row = await connection.QuerySingleOrDefaultAsync<MainRow>(
            new CommandDefinition(
                sql,
                new
                {
                    FindingId = findingId.Trim(),
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId
                },
                cancellationToken: ct));

        if (row is null)
            return null;


        const string relatedSql = """
                                  SELECT frn.NodeId
                                  FROM dbo.FindingRelatedNodes frn
                                  INNER JOIN dbo.FindingRecords fr ON fr.FindingRecordId = frn.FindingRecordId
                                  INNER JOIN dbo.FindingsSnapshots fs ON fs.FindingsSnapshotId = fr.FindingsSnapshotId
                                  INNER JOIN dbo.Runs r ON r.RunId = fs.RunId
                                  WHERE fr.FindingId = @FindingId
                                    AND r.TenantId = @TenantId
                                    AND r.WorkspaceId = @WorkspaceId
                                    AND r.ScopeProjectId = @ScopeProjectId
                                  ORDER BY frn.SortOrder;
                                  """;

        List<string> relatedNodes = (await connection.QueryAsync<string>(
                new CommandDefinition(
                    relatedSql,
                    new
                    {
                        FindingId = findingId.Trim(),
                        scope.TenantId,
                        scope.WorkspaceId,
                        ScopeProjectId = scope.ProjectId
                    },
                    cancellationToken: ct)))
            .ToList();


        const string ruleSql = """
                               SELECT TOP 1 tra.RuleText
                               FROM dbo.FindingTraceRulesApplied tra
                               INNER JOIN dbo.FindingRecords fr ON fr.FindingRecordId = tra.FindingRecordId
                               INNER JOIN dbo.FindingsSnapshots fs ON fs.FindingsSnapshotId = fr.FindingsSnapshotId
                               INNER JOIN dbo.Runs r ON r.RunId = fs.RunId
                               WHERE fr.FindingId = @FindingId
                                 AND r.TenantId = @TenantId
                                 AND r.WorkspaceId = @WorkspaceId
                                 AND r.ScopeProjectId = @ScopeProjectId
                               ORDER BY tra.SortOrder;
                               """;

        string? firstRuleText = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                ruleSql,
                new
                {
                    FindingId = findingId.Trim(),
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId
                },
                cancellationToken: ct));


        const string actionsSql = """
                                  SELECT fra.ActionText
                                  FROM dbo.FindingRecommendedActions fra
                                  INNER JOIN dbo.FindingRecords fr ON fr.FindingRecordId = fra.FindingRecordId
                                  INNER JOIN dbo.FindingsSnapshots fs ON fs.FindingsSnapshotId = fr.FindingsSnapshotId
                                  INNER JOIN dbo.Runs r ON r.RunId = fs.RunId
                                  WHERE fr.FindingId = @FindingId
                                    AND r.TenantId = @TenantId
                                    AND r.WorkspaceId = @WorkspaceId
                                    AND r.ScopeProjectId = @ScopeProjectId
                                  ORDER BY fra.SortOrder;
                                  """;

        List<string> recommendedActions = (await connection.QueryAsync<string>(
                new CommandDefinition(
                    actionsSql,
                    new
                    {
                        FindingId = findingId.Trim(),
                        scope.TenantId,
                        scope.WorkspaceId,
                        ScopeProjectId = scope.ProjectId
                    },
                    cancellationToken: ct)))
            .Where(static a => !string.IsNullOrWhiteSpace(a))
            .ToList();


        const string auditSql = """
                                SELECT TOP 1 ae.EventId
                                FROM dbo.AuditEvents ae
                                WHERE ae.RunId = @RunId
                                  AND ae.TenantId = @TenantId
                                  AND ae.EventType = @EventType
                                ORDER BY ae.OccurredUtc DESC, ae.EventId DESC;
                                """;

        Guid? auditRowId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                auditSql,
                new { row.RunId, scope.TenantId, EventType = AuditEventTypes.AuthorityCommittedChainPersisted },
                cancellationToken: ct));

        (string? ruleId, string? ruleName) = ResolveRuleFields(row.AppliedRuleIdsJson, firstRuleText);

        FindingHumanReviewStatus humanReview = FindingInspectReadModelMapper.ParseHumanReview(row.HumanReviewStatus);

        FindingConfidenceLevel? evaluationLevel =
            FindingInspectReadModelMapper.TryParseEvaluationConfidenceLevel(row.EvaluationConfidenceLevel);

        List<FindingInspectEvidenceItem> evidence = relatedNodes
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .Select(static n =>
                new FindingInspectEvidenceItem { ArtifactId = null, LineRange = null, Excerpt = n.Trim() })
            .ToList();

        JsonElement? typed = TryParsePayloadJson(row.PayloadJson);

        return new FindingInspectResponse
        {
            FindingId = row.FindingId,
            TypedPayload = typed,
            DecisionRuleId = ruleId,
            DecisionRuleName = ruleName ?? ruleId,
            Evidence = evidence,
            RecommendedActions = recommendedActions,
            AuditRowId = auditRowId,
            RunId = row.RunId,
            ManifestVersion = row.CurrentManifestVersion,
            ModelDeploymentName = row.ModelDeploymentName,
            PromptTemplateVersion = row.PromptTemplateVersion,
            ConfidenceScore = row.ConfidenceScore,
            EvaluationConfidenceScore = row.EvaluationConfidenceScore,
            ConfidenceLevel = evaluationLevel,
            HumanReviewStatus = humanReview
        };
    }

    private static (string? RuleId, string? RuleName) ResolveRuleFields(string? appliedRuleIdsJson,
        string? firstRuleText)
    {
        if (!string.IsNullOrWhiteSpace(appliedRuleIdsJson))
        {
            try
            {
                List<string>? ids = JsonSerializer.Deserialize<List<string>>(appliedRuleIdsJson);

                if (ids is { Count: > 0 })
                {
                    string first = ids[0].Trim();

                    if (first.Length > 0)
                        return (first, string.IsNullOrWhiteSpace(firstRuleText) ? first : firstRuleText.Trim());
                }
            }
            catch (JsonException)
            {
                // Fall through to trace text only.
            }
        }

        if (!string.IsNullOrWhiteSpace(firstRuleText))
            return (firstRuleText.Trim(), firstRuleText.Trim());

        return (null, null);
    }

    private static JsonElement? TryParsePayloadJson(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(payloadJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class MainRow
    {
        public string FindingId
        {
            get;
            init;
        } = string.Empty;

        public string? PayloadJson
        {
            get;
            init;
        }

        public Guid RunId
        {
            get;
            init;
        }

        public string? CurrentManifestVersion
        {
            get;
            init;
        }

        public Guid? GoldenManifestId
        {
            get;
            init;
        }

        public string? AppliedRuleIdsJson
        {
            get;
            init;
        }

        public string? ModelDeploymentName
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

        public string? HumanReviewStatus
        {
            get;
            init;
        }
    }
}
