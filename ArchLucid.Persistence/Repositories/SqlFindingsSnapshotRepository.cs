using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Findings.Serialization;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Findings;
using ArchLucid.Persistence.RelationalRead;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Telemetry;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     SQL Server-backed <see cref="IFindingsSnapshotRepository" /> with dual-write to <c>FindingsJson</c> and relational
///     finding tables; reads prefer <c>dbo.FindingRecords</c> and fall back to <c>FindingsJson</c> when no rows exist.
///     Typed <see cref="Finding.Payload" /> is stored only in <c>FindingRecords.PayloadJson</c> (sidecar). All other
///     finding
///     fields and trace lists are relational with stable <c>SortOrder</c>. <see cref="FindingsSnapshotMigrator" /> runs on
///     save and after load so schema versioning stays consistent.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class SqlFindingsSnapshotRepository(
    ISqlConnectionFactory connectionFactory,
    IScopeContextProvider scopeContextProvider) : IFindingsSnapshotRepository
{
    private const int FindingChildTripleColumnInsertRows = 650;
    private const int FindingChildPropertyInsertRows = 650;

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

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

    public async Task<FindingsSnapshot?> GetByIdAsync(Guid findingsSnapshotId, CancellationToken ct)
    {
        const string sql = """
                           SELECT
                               FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                               SchemaVersion, GenerationStatus, FindingsJson
                           FROM dbo.FindingsSnapshots
                           WHERE FindingsSnapshotId = @FindingsSnapshotId;
                           """;

        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            FindingsSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<FindingsSnapshotStorageRow>(
                new CommandDefinition(
                    sql,
                    new { FindingsSnapshotId = findingsSnapshotId },
                    cancellationToken: ct));

            if (row is null)
                return null;

            int recordCount = await SqlRelationalScalarCount.ExecuteAsync(
                connection,
                null,
                "SELECT COUNT(1) FROM dbo.FindingRecords WHERE FindingsSnapshotId = @FindingsSnapshotId",
                new { FindingsSnapshotId = findingsSnapshotId },
                ct);

            if (recordCount == 0)
            {
                if (string.IsNullOrWhiteSpace(row.FindingsJson))

                    return new FindingsSnapshot
                    {
                        FindingsSnapshotId = row.FindingsSnapshotId,
                        RunId = row.RunId,
                        ContextSnapshotId = row.ContextSnapshotId,
                        GraphSnapshotId = row.GraphSnapshotId,
                        CreatedUtc = row.CreatedUtc,
                        SchemaVersion = row.SchemaVersion,
                        GenerationStatus = FindingsSnapshotGenerationStatusParser.Parse(row.GenerationStatus),
                        Findings = []
                    };


                FindingsSnapshot fromJson = JsonEntitySerializer.Deserialize<FindingsSnapshot>(row.FindingsJson);
                fromJson.FindingsSnapshotId = row.FindingsSnapshotId;
                fromJson.RunId = row.RunId;
                fromJson.ContextSnapshotId = row.ContextSnapshotId;
                fromJson.GraphSnapshotId = row.GraphSnapshotId;
                fromJson.CreatedUtc = row.CreatedUtc;
                fromJson.SchemaVersion = row.SchemaVersion;
                FindingsSnapshotMigrator.Apply(fromJson);
                FindingPayloadJsonCodec.HydrateJsonElementPayloads(fromJson.Findings);
                return fromJson;
            }

            FindingsSnapshot snapshot =
                await FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync(connection, row, ct);
            FindingsSnapshotMigrator.Apply(snapshot);
            return snapshot;
        }
        finally
        {
            ArchLucidInstrumentation.RecordNamedQueryLatencyMilliseconds(
                NamedQueryTelemetryNames.GetFindingsSnapshotById,
                sw.Elapsed.TotalMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task<FindingRecordMetadataPage> ListFindingRecordsKeysetAsync(
        Guid findingsSnapshotId,
        int? cursorSortOrder,
        Guid? cursorFindingRecordId,
        string? severity,
        string? category,
        string? findingType,
        int take,
        CancellationToken ct)
    {
        if (cursorSortOrder.HasValue ^ cursorFindingRecordId.HasValue)
            throw new ArgumentException("Cursor requires both sortOrder and findingRecordId, or neither for the first page.");

        int cappedTake = Math.Clamp(take <= 0 ? FindingPagination.DefaultTake : take, 1, FindingPagination.MaxTake);
        int fetchLimit = cappedTake + 1;

        const string sql = """
                             SELECT TOP (@Limit)
                                    FindingRecordId, SortOrder, FindingId, FindingType, Category, EngineType, Severity, Title
                             FROM dbo.FindingRecords
                             WHERE FindingsSnapshotId = @FsId
                               AND (@Severity IS NULL OR Severity = @Severity)
                               AND (@Category IS NULL OR Category = @Category)
                               AND (@FindingType IS NULL OR FindingType = @FindingType)
                               AND (
                                 @HasCursor = 0
                                 OR (
                                   SortOrder > @CurSo OR (SortOrder = @CurSo AND FindingRecordId > @CurFrid)
                                 )
                               )
                             ORDER BY SortOrder ASC, FindingRecordId ASC;
                             """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        bool hasCursor = cursorSortOrder.HasValue && cursorFindingRecordId.HasValue;

        List<FindingMetaSqlRow> rows = (
            await connection.QueryAsync<FindingMetaSqlRow>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        FsId = findingsSnapshotId,
                        Severity = OptionalEqualityFilter(severity),
                        Category = OptionalEqualityFilter(category),
                        FindingType = OptionalEqualityFilter(findingType),
                        HasCursor = hasCursor ? 1 : 0,
                        CurSo = cursorSortOrder ?? 0,
                        CurFrid = cursorFindingRecordId ?? Guid.Empty,
                        Limit = fetchLimit
                    },
                    cancellationToken: ct))).ToList();

        bool hasMore = rows.Count > cappedTake;

        if (hasMore)

            rows.RemoveAt(rows.Count - 1);

        FindingRecordMetadataRow[] mapped =
            rows.ConvertAll(static r =>
                new FindingRecordMetadataRow(
                    r.FindingRecordId,
                    r.SortOrder,
                    r.FindingId,
                    r.FindingType,
                    r.Category,
                    r.EngineType,
                    r.Severity,
                    r.Title)).ToArray();

        return new FindingRecordMetadataPage(mapped, hasMore);
    }

    private sealed class FindingMetaSqlRow
    {
        public Guid FindingRecordId { get; init; }

        public int SortOrder { get; init; }

        public string FindingId { get; init; } = null!;

        public string FindingType { get; init; } = null!;

        public string Category { get; init; } = null!;

        public string EngineType { get; init; } = null!;

        public string Severity { get; init; } = null!;

        public string Title { get; init; } = null!;
    }

    private async Task SaveCoreAsync(
        FindingsSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        FindingsSnapshotMigrator.Apply(snapshot);

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();

        const string headerSql = """
                                 INSERT INTO dbo.FindingsSnapshots
                                 (
                                     FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId,
                                     TenantId, WorkspaceId, ProjectId,
                                     CreatedUtc, SchemaVersion, GenerationStatus, FindingsJson
                                 )
                                 VALUES
                                 (
                                     @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId,
                                     @TenantId, @WorkspaceId, @ProjectId,
                                     @CreatedUtc, @SchemaVersion, @GenerationStatus, @FindingsJson
                                 );
                                 """;

        object headerArgs = new
        {
            snapshot.FindingsSnapshotId,
            snapshot.RunId,
            snapshot.ContextSnapshotId,
            snapshot.GraphSnapshotId,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            snapshot.CreatedUtc,
            snapshot.SchemaVersion,
            GenerationStatus = snapshot.GenerationStatus.ToString(),
            FindingsJson = JsonEntitySerializer.Serialize(snapshot)
        };

        await connection.ExecuteAsync(new CommandDefinition(headerSql, headerArgs, transaction, cancellationToken: ct))
            ;

        await InsertFindingsRelationalFromSnapshotAsync(
            snapshot,
            connection,
            transaction,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);
    }

    internal static async Task InsertFindingsRelationalFromSnapshotAsync(
        FindingsSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
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
                i,
                finding,
                tenantId,
                workspaceId,
                projectId,
                ct);

            await InsertFindingChildrenAsync(
                connection,
                transaction,
                recordId,
                finding,
                tenantId,
                workspaceId,
                projectId,
                ct);
        }
    }

    private static async Task InsertFindingRecordAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid findingsSnapshotId,
        Guid findingRecordId,
        int sortOrder,
        Finding finding,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
                           INSERT INTO dbo.FindingRecords
                           (
                               FindingRecordId, FindingsSnapshotId, SortOrder,
                               TenantId, WorkspaceId, ProjectId,
                               FindingId, FindingSchemaVersion, FindingType, Category, EngineType,
                               Severity, Title, Rationale, PayloadType, PayloadJson,
                               RequestInputRef, RunIdRef, AgentExecutionTraceId,
                               ModelDeploymentName, ModelVersion, PromptTemplateId, PromptTemplateVersion,
                               ConfidenceScore, EvaluationConfidenceScore, EvaluationConfidenceLevel, PolicyRuleId,
                               HumanReviewStatus, ReviewedByUserId, ReviewedAtUtc, ReviewNotes
                           )
                           VALUES
                           (
                               @FindingRecordId, @FindingsSnapshotId, @SortOrder,
                               @TenantId, @WorkspaceId, @ProjectId,
                               @FindingId, @FindingSchemaVersion, @FindingType, @Category, @EngineType,
                               @Severity, @Title, @Rationale, @PayloadType, @PayloadJson,
                               @RequestInputRef, @RunIdRef, @AgentExecutionTraceId,
                               @ModelDeploymentName, @ModelVersion, @PromptTemplateId, @PromptTemplateVersion,
                               @ConfidenceScore, @EvaluationConfidenceScore, @EvaluationConfidenceLevel, @PolicyRuleId,
                               @HumanReviewStatus, @ReviewedByUserId, @ReviewedAtUtc, @ReviewNotes
                           );
                           """;

        object args = new
        {
            FindingRecordId = findingRecordId,
            FindingsSnapshotId = findingsSnapshotId,
            SortOrder = sortOrder,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
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
            RequestInputRef = finding.RequestInputRef,
            RunIdRef = finding.RunIdRef,
            AgentExecutionTraceId = finding.AgentExecutionTraceId ?? finding.Trace.SourceAgentExecutionTraceId,
            finding.ModelDeploymentName,
            finding.ModelVersion,
            finding.PromptTemplateId,
            finding.PromptTemplateVersion,
            finding.ConfidenceScore,
            EvaluationConfidenceScore = finding.EvaluationConfidenceScore,
            EvaluationConfidenceLevel = finding.ConfidenceLevel is { } lvl ? lvl.ToString() : null,
            finding.PolicyRuleId,
            HumanReviewStatus = finding.HumanReviewStatus.ToString(),
            finding.ReviewedByUserId,
            ReviewedAtUtc = finding.ReviewedAtUtc,
            finding.ReviewNotes
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));
    }

    private static async Task InsertFindingChildrenAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid findingRecordId,
        Finding finding,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        await InsertTripleStringColumnChunksAsync(
            connection,
            transaction,
            """
            INSERT INTO dbo.FindingRelatedNodes (FindingRecordId, SortOrder, NodeId, TenantId, WorkspaceId, ProjectId)
            VALUES
            """,
            findingRecordId,
            finding.RelatedNodeIds,
            tenantId,
            workspaceId,
            projectId,
            ct);

        await InsertTripleStringColumnChunksAsync(
            connection,
            transaction,
            """
            INSERT INTO dbo.FindingRecommendedActions (FindingRecordId, SortOrder, ActionText, TenantId, WorkspaceId, ProjectId)
            VALUES
            """,
            findingRecordId,
            finding.RecommendedActions,
            tenantId,
            workspaceId,
            projectId,
            ct);

        List<KeyValuePair<string, string>> orderedProps = finding.Properties
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        await InsertFindingPropertiesChunksAsync(
            connection,
            transaction,
            findingRecordId,
            orderedProps,
            tenantId,
            workspaceId,
            projectId,
            ct);

        await InsertTripleStringColumnChunksAsync(
            connection,
            transaction,
            """
            INSERT INTO dbo.FindingTraceGraphNodesExamined (FindingRecordId, SortOrder, NodeId, TenantId, WorkspaceId, ProjectId)
            VALUES
            """,
            findingRecordId,
            finding.Trace.GraphNodeIdsExamined,
            tenantId,
            workspaceId,
            projectId,
            ct);

        await InsertTripleStringColumnChunksAsync(
            connection,
            transaction,
            """
            INSERT INTO dbo.FindingTraceRulesApplied (FindingRecordId, SortOrder, RuleText, TenantId, WorkspaceId, ProjectId)
            VALUES
            """,
            findingRecordId,
            finding.Trace.RulesApplied,
            tenantId,
            workspaceId,
            projectId,
            ct);

        await InsertTripleStringColumnChunksAsync(
            connection,
            transaction,
            """
            INSERT INTO dbo.FindingTraceDecisionsTaken (FindingRecordId, SortOrder, DecisionText, TenantId, WorkspaceId, ProjectId)
            VALUES
            """,
            findingRecordId,
            finding.Trace.DecisionsTaken,
            tenantId,
            workspaceId,
            projectId,
            ct);

        await InsertTripleStringColumnChunksAsync(
            connection,
            transaction,
            """
            INSERT INTO dbo.FindingTraceAlternativePaths (FindingRecordId, SortOrder, PathText, TenantId, WorkspaceId, ProjectId)
            VALUES
            """,
            findingRecordId,
            finding.Trace.AlternativePathsConsidered,
            tenantId,
            workspaceId,
            projectId,
            ct);

        await InsertTripleStringColumnChunksAsync(
            connection,
            transaction,
            """
            INSERT INTO dbo.FindingTraceNotes (FindingRecordId, SortOrder, NoteText, TenantId, WorkspaceId, ProjectId)
            VALUES
            """,
            findingRecordId,
            finding.Trace.Notes,
            tenantId,
            workspaceId,
            projectId,
            ct);
    }

    private static async Task InsertTripleStringColumnChunksAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string insertHeaderThroughValuesKeyword,
        Guid findingRecordId,
        List<string> rows,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        if (rows.Count == 0)
            return;

        for (int offset = 0; offset < rows.Count; offset += FindingChildTripleColumnInsertRows)
        {
            int len = Math.Min(FindingChildTripleColumnInsertRows, rows.Count - offset);
            StringBuilder sb = new StringBuilder(insertHeaderThroughValuesKeyword.Length + len * 80);
            sb.Append(insertHeaderThroughValuesKeyword);
            DynamicParameters dp = new();
            dp.Add("fid", findingRecordId, DbType.Guid);
            dp.Add("tid", tenantId);
            dp.Add("wid", workspaceId);
            dp.Add("pid", projectId);

            for (int i = 0; i < len; i++)
            {
                if (i > 0)
                    sb.Append(',');

                int sortOrder = offset + i;
                sb.Append($"(@fid,@s{i},@t{i},@tid,@wid,@pid)");
                dp.Add($"s{i}", sortOrder);
                dp.Add($"t{i}", rows[sortOrder]);
            }

            await connection.ExecuteAsync(
                new CommandDefinition(sb.ToString(), dp, transaction, cancellationToken: ct));
        }
    }

    private static async Task InsertFindingPropertiesChunksAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid findingRecordId,
        List<KeyValuePair<string, string>> orderedProps,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        if (orderedProps.Count == 0)
            return;

        const string preamble = """
            INSERT INTO dbo.FindingProperties (
                FindingRecordId, PropertySortOrder, PropertyKey, PropertyValue, TenantId, WorkspaceId, ProjectId)
            VALUES
            """;

        for (int offset = 0; offset < orderedProps.Count; offset += FindingChildPropertyInsertRows)
        {
            int len = Math.Min(FindingChildPropertyInsertRows, orderedProps.Count - offset);
            StringBuilder sb = new StringBuilder(preamble.Length + len * 96);
            sb.Append(preamble);
            DynamicParameters dp = new();
            dp.Add("fid", findingRecordId, DbType.Guid);
            dp.Add("tid", tenantId);
            dp.Add("wid", workspaceId);
            dp.Add("pid", projectId);

            for (int i = 0; i < len; i++)
            {
                if (i > 0)
                    sb.Append(',');

                KeyValuePair<string, string> kv = orderedProps[offset + i];
                sb.Append($"(@fid,@ps{i},@pk{i},@pv{i},@tid,@wid,@pid)");
                dp.Add($"ps{i}", offset + i);
                dp.Add($"pk{i}", kv.Key);
                dp.Add($"pv{i}", kv.Value);
            }

            await connection.ExecuteAsync(
                new CommandDefinition(sb.ToString(), dp, transaction, cancellationToken: ct));
        }
    }

    /// <summary>
    ///     Inserts relational finding rows when <c>FindingRecords</c> is still empty (idempotent).
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
            new { snapshot.FindingsSnapshotId },
            ct);

        if (recordCount > 0 || snapshot.Findings.Count == 0)
            return;

        FindingsSnapshotMigrator.Apply(snapshot);

        const string scopeSql = """
                                SELECT TenantId, WorkspaceId, ProjectId
                                FROM dbo.FindingsSnapshots
                                WHERE FindingsSnapshotId = @FindingsSnapshotId;
                                """;

        FindingSnapshotScopeRow? scopeHdr = await connection.QuerySingleOrDefaultAsync<FindingSnapshotScopeRow>(
            new CommandDefinition(scopeSql, new { snapshot.FindingsSnapshotId }, transaction, cancellationToken: ct));

        if (scopeHdr?.TenantId is null || scopeHdr.WorkspaceId is null || scopeHdr.ProjectId is null)
            throw new InvalidOperationException(
                $"dbo.FindingsSnapshots row {snapshot.FindingsSnapshotId} lacks denormalized RLS scope (tenant/workspace/project); cannot backfill FindingRecords.");

        await InsertFindingsRelationalFromSnapshotAsync(
            snapshot,
            connection,
            transaction,
            scopeHdr.TenantId!.Value,
            scopeHdr.WorkspaceId!.Value,
            scopeHdr.ProjectId!.Value,
            ct);
    }

    private sealed record FindingSnapshotScopeRow(Guid? TenantId, Guid? WorkspaceId, Guid? ProjectId);

    private static string? OptionalEqualityFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
