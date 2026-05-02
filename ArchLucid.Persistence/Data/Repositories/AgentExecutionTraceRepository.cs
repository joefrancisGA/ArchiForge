using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Dapper-backed persistence for <see cref="AgentExecutionTrace" /> entities.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class AgentExecutionTraceRepository(IDbConnectionFactory connectionFactory)
    : IAgentExecutionTraceRepository
{
    public async Task CreateAsync(
        AgentExecutionTrace trace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trace);

        const string sql = """
                           INSERT INTO AgentExecutionTraces
                           (
                               TraceId,
                               RunId,
                               TaskId,
                               AgentType,
                               ParseSucceeded,
                               ErrorMessage,
                               TraceJson,
                               CreatedUtc,
                               FullSystemPromptBlobKey,
                               FullUserPromptBlobKey,
                               FullResponseBlobKey,
                               ModelDeploymentName,
                               ModelVersion
                           )
                           VALUES
                           (
                               @TraceId,
                               @RunId,
                               @TaskId,
                               @AgentType,
                               @ParseSucceeded,
                               @ErrorMessage,
                               @TraceJson,
                               @CreatedUtc,
                               @FullSystemPromptBlobKey,
                               @FullUserPromptBlobKey,
                               @FullResponseBlobKey,
                               @ModelDeploymentName,
                               @ModelVersion
                           );
                           """;

        string json = JsonSerializer.Serialize(trace, ContractJson.Default);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                trace.TraceId,
                trace.RunId,
                trace.TaskId,
                AgentType = trace.AgentType.ToString(),
                trace.ParseSucceeded,
                trace.ErrorMessage,
                TraceJson = json,
                trace.CreatedUtc,
                trace.FullSystemPromptBlobKey,
                trace.FullUserPromptBlobKey,
                trace.FullResponseBlobKey,
                trace.ModelDeploymentName,
                trace.ModelVersion
            },
            cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task PatchBlobStorageFieldsAsync(
        string traceId,
        string? fullSystemPromptBlobKey,
        string? fullUserPromptBlobKey,
        string? fullResponseBlobKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string selectSql = """
                                 SELECT TraceJson
                                 FROM AgentExecutionTraces
                                 WHERE TraceId = @TraceId;
                                 """;

        string? rowJson = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(selectSql, new
            {
                TraceId = traceId
            }, cancellationToken: cancellationToken));

        if (string.IsNullOrEmpty(rowJson))
            return;


        AgentExecutionTrace? trace = JsonSerializer.Deserialize<AgentExecutionTrace>(rowJson, ContractJson.Default);
        if (trace is null)
            return;


        if (fullSystemPromptBlobKey is not null)

            trace.FullSystemPromptBlobKey = fullSystemPromptBlobKey;


        if (fullUserPromptBlobKey is not null)

            trace.FullUserPromptBlobKey = fullUserPromptBlobKey;


        if (fullResponseBlobKey is not null)

            trace.FullResponseBlobKey = fullResponseBlobKey;


        string updatedJson = JsonSerializer.Serialize(trace, ContractJson.Default);

        const string updateSql = """
                                 UPDATE AgentExecutionTraces
                                 SET FullSystemPromptBlobKey = @FullSystemPromptBlobKey,
                                     FullUserPromptBlobKey = @FullUserPromptBlobKey,
                                     FullResponseBlobKey = @FullResponseBlobKey,
                                     TraceJson = @TraceJson
                                 WHERE TraceId = @TraceId;
                                 """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    TraceId = traceId,
                    trace.FullSystemPromptBlobKey,
                    trace.FullUserPromptBlobKey,
                    trace.FullResponseBlobKey,
                    TraceJson = updatedJson
                },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task PatchBlobUploadFailedAsync(
        string traceId,
        bool failed,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        const string sql = """
                           UPDATE AgentExecutionTraces
                           SET BlobUploadFailed = @BlobUploadFailed
                           WHERE TraceId = @TraceId;
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    TraceId = traceId,
                    BlobUploadFailed = failed
                },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task PatchInlinePromptFallbackAsync(
        string traceId,
        string? fullSystemPromptInline,
        string? fullUserPromptInline,
        string? fullResponseInline,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string selectSql = """
                                 SELECT TraceJson
                                 FROM AgentExecutionTraces
                                 WHERE TraceId = @TraceId;
                                 """;

        string? rowJson = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(selectSql, new
            {
                TraceId = traceId
            }, cancellationToken: cancellationToken));

        if (string.IsNullOrEmpty(rowJson))
            return;


        AgentExecutionTrace? trace = JsonSerializer.Deserialize<AgentExecutionTrace>(rowJson, ContractJson.Default);
        if (trace is null)
            return;


        if (fullSystemPromptInline is not null)

            trace.FullSystemPromptInline = fullSystemPromptInline;


        if (fullUserPromptInline is not null)

            trace.FullUserPromptInline = fullUserPromptInline;


        if (fullResponseInline is not null)

            trace.FullResponseInline = fullResponseInline;


        string updatedJson = JsonSerializer.Serialize(trace, ContractJson.Default);

        const string updateSql = """
                                 UPDATE AgentExecutionTraces
                                 SET FullSystemPromptInline = COALESCE(@FullSystemPromptInline, FullSystemPromptInline),
                                     FullUserPromptInline = COALESCE(@FullUserPromptInline, FullUserPromptInline),
                                     FullResponseInline = COALESCE(@FullResponseInline, FullResponseInline),
                                     TraceJson = @TraceJson
                                 WHERE TraceId = @TraceId;
                                 """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    TraceId = traceId,
                    FullSystemPromptInline = fullSystemPromptInline,
                    FullUserPromptInline = fullUserPromptInline,
                    FullResponseInline = fullResponseInline,
                    TraceJson = updatedJson
                },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task PatchInlineFallbackFailedAsync(
        string traceId,
        bool failed,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string selectSql = """
                                 SELECT TraceJson
                                 FROM AgentExecutionTraces
                                 WHERE TraceId = @TraceId;
                                 """;

        string? rowJson = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(selectSql, new
            {
                TraceId = traceId
            }, cancellationToken: cancellationToken));

        if (string.IsNullOrEmpty(rowJson))
            return;


        AgentExecutionTrace? trace = JsonSerializer.Deserialize<AgentExecutionTrace>(rowJson, ContractJson.Default);
        if (trace is null)
            return;


        trace.InlineFallbackFailed = failed ? true : null;

        string updatedJson = JsonSerializer.Serialize(trace, ContractJson.Default);

        const string updateSql = """
                                 UPDATE AgentExecutionTraces
                                 SET InlineFallbackFailed = @InlineFallbackFailed,
                                     TraceJson = @TraceJson
                                 WHERE TraceId = @TraceId;
                                 """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    TraceId = traceId,
                    InlineFallbackFailed = failed ? true : (bool?)null,
                    TraceJson = updatedJson
                },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task PatchQualityWarningAsync(
        string traceId,
        bool qualityWarning,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string selectSql = """
                                 SELECT TraceJson
                                 FROM AgentExecutionTraces
                                 WHERE TraceId = @TraceId;
                                 """;

        string? rowJson = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(selectSql, new
            {
                TraceId = traceId
            }, cancellationToken: cancellationToken));

        if (string.IsNullOrEmpty(rowJson))
            return;


        AgentExecutionTrace? trace = JsonSerializer.Deserialize<AgentExecutionTrace>(rowJson, ContractJson.Default);
        if (trace is null)
            return;


        trace.QualityWarning = qualityWarning;

        string updatedJson = JsonSerializer.Serialize(trace, ContractJson.Default);

        const string updateSql = """
                                 UPDATE AgentExecutionTraces
                                 SET TraceJson = @TraceJson
                                 WHERE TraceId = @TraceId;
                                 """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    TraceId = traceId,
                    TraceJson = updatedJson
                },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AgentExecutionTrace?> GetByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT TraceJson
                           FROM AgentExecutionTraces
                           WHERE TraceId = @TraceId;
                           """;

        string? rowJson = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(sql, new
            {
                TraceId = traceId
            }, cancellationToken: cancellationToken));

        return string.IsNullOrEmpty(rowJson) ? null : JsonSerializer.Deserialize<AgentExecutionTrace>(rowJson, ContractJson.Default);
    }

    public async Task<IReadOnlyList<AgentExecutionTrace>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
                      SELECT TraceJson
                      FROM AgentExecutionTraces
                      WHERE RunId = @RunId
                      ORDER BY CreatedUtc
                      {SqlPagingSyntax.FirstRowsOnly(500)};
                      """;

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId
            },
            cancellationToken: cancellationToken));

        return DeserializeTraces(rows, $"run '{runId}'");
    }

    public async Task<(IReadOnlyList<AgentExecutionTrace> Traces, int TotalCount)> GetPagedByRunIdAsync(
        string runId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT TraceJson,
                                  COUNT(*) OVER () AS TotalCount
                           FROM AgentExecutionTraces
                           WHERE RunId = @RunId
                           ORDER BY CreatedUtc
                           OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
                           """;

        int clampedOffset = Math.Max(0, offset);
        int clampedLimit = Math.Clamp(limit, 1, 500);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IEnumerable<TracePageRow> rows = await connection.QueryAsync<TracePageRow>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId,
                Offset = clampedOffset,
                Limit = clampedLimit
            },
            cancellationToken: cancellationToken));

        List<TracePageRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? list[0].TotalCount : 0;

        IReadOnlyList<AgentExecutionTrace> traces =
            DeserializeTraces(list.Select(row => row.TraceJson), $"run '{runId}' (paged)");

        return (traces, totalCount);
    }

    public async Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
                      SELECT TraceJson
                      FROM AgentExecutionTraces
                      WHERE TaskId = @TaskId
                      ORDER BY CreatedUtc
                      {SqlPagingSyntax.FirstRowsOnly(500)};
                      """;

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new
            {
                TaskId = taskId
            },
            cancellationToken: cancellationToken));

        return DeserializeTraces(rows, $"task '{taskId}'");
    }

    private static IReadOnlyList<AgentExecutionTrace> DeserializeTraces(
        IEnumerable<string> jsonRows,
        string context)
    {
        List<AgentExecutionTrace> traces = [];
        foreach (string json in jsonRows)
        {
            AgentExecutionTrace? trace;
            try
            {
                trace = JsonSerializer.Deserialize<AgentExecutionTrace>(json, ContractJson.Default);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize an AgentExecutionTrace for {context}. " +
                    "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
            }

            if (trace is null)

                throw new InvalidOperationException(
                    $"An AgentExecutionTrace row for {context} deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");


            traces.Add(trace);
        }

        return traces;
    }

    private sealed class TracePageRow
    {
        public string TraceJson
        {
            get;
            init;
        } = string.Empty;

        public int TotalCount
        {
            get;
            init;
        }
    }
}
