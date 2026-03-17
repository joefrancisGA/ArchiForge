using System.Data;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Infrastructure;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ArchiForge.Data.Repositories;

public sealed class ComparisonRecordRepository : IComparisonRecordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ComparisonRecordRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        Infrastructure.ListStringTypeHandler.Register();
    }

    public async Task CreateAsync(
        ComparisonRecord record,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO ComparisonRecords
            (
                ComparisonRecordId,
                ComparisonType,
                LeftRunId,
                RightRunId,
                LeftManifestVersion,
                RightManifestVersion,
                LeftExportRecordId,
                RightExportRecordId,
                Format,
                SummaryMarkdown,
                PayloadJson,
                Notes,
                CreatedUtc,
                Label,
                Tags
            )
            VALUES
            (
                @ComparisonRecordId,
                @ComparisonType,
                @LeftRunId,
                @RightRunId,
                @LeftManifestVersion,
                @RightManifestVersion,
                @LeftExportRecordId,
                @RightExportRecordId,
                @Format,
                @SummaryMarkdown,
                @PayloadJson,
                @Notes,
                @CreatedUtc,
                @Label,
                @Tags
            );
            """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            record,
            cancellationToken: cancellationToken));
    }

    public async Task<ComparisonRecord?> GetByIdAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 1 *
            FROM ComparisonRecords
            WHERE ComparisonRecordId = @ComparisonRecordId;
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<ComparisonRecord>(new CommandDefinition(
            sql,
            new { ComparisonRecordId = comparisonRecordId },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ComparisonRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM ComparisonRecords
            WHERE LeftRunId = @RunId OR RightRunId = @RunId
            ORDER BY CreatedUtc DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<ComparisonRecord>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<ComparisonRecord>> GetByExportRecordIdAsync(
        string exportRecordId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM ComparisonRecords
            WHERE LeftExportRecordId = @ExportRecordId OR RightExportRecordId = @ExportRecordId
            ORDER BY CreatedUtc DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<ComparisonRecord>(new CommandDefinition(
            sql,
            new { ExportRecordId = exportRecordId },
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<ComparisonRecord>> SearchAsync(
        string? comparisonType,
        string? leftRunId,
        string? rightRunId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? leftExportRecordId,
        string? rightExportRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        string? sortBy,
        string? sortDir,
        int skip,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string baseSql = """
            SELECT *
            FROM ComparisonRecords
            WHERE 1 = 1
            """;

        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        var safeLimit = limit <= 0 ? 50 : Math.Min(limit, 500);
        var safeSkip = skip < 0 ? 0 : skip;
        parameters.Add("@Limit", safeLimit);
        parameters.Add("@Skip", safeSkip);

        if (!string.IsNullOrWhiteSpace(comparisonType))
        {
            conditions.Add("ComparisonType = @ComparisonType");
            parameters.Add("@ComparisonType", comparisonType);
        }

        if (!string.IsNullOrWhiteSpace(leftRunId))
        {
            conditions.Add("LeftRunId = @LeftRunId");
            parameters.Add("@LeftRunId", leftRunId);
        }

        if (!string.IsNullOrWhiteSpace(rightRunId))
        {
            conditions.Add("RightRunId = @RightRunId");
            parameters.Add("@RightRunId", rightRunId);
        }

        if (createdFromUtc is not null)
        {
            conditions.Add("CreatedUtc >= @CreatedFromUtc");
            parameters.Add("@CreatedFromUtc", createdFromUtc);
        }

        if (createdToUtc is not null)
        {
            conditions.Add("CreatedUtc <= @CreatedToUtc");
            parameters.Add("@CreatedToUtc", createdToUtc);
        }

        using var connection = _connectionFactory.CreateConnection();
        if (!string.IsNullOrWhiteSpace(leftExportRecordId))
        {
            conditions.Add("LeftExportRecordId = @LeftExportRecordId");
            parameters.Add("@LeftExportRecordId", leftExportRecordId);
        }

        if (!string.IsNullOrWhiteSpace(rightExportRecordId))
        {
            conditions.Add("RightExportRecordId = @RightExportRecordId");
            parameters.Add("@RightExportRecordId", rightExportRecordId);
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            conditions.Add("Label = @Label");
            parameters.Add("@Label", label);
        }

        var isSqlite = IsSqlite(connection);
        if (tags is { Count: > 0 })
        {
            for (var i = 0; i < tags.Count; i++)
            {
                var t = tags[i];
                if (string.IsNullOrWhiteSpace(t)) continue;
                var paramName = $"@Tag{i}";
                parameters.Add(paramName, t);
                conditions.Add(isSqlite
                    ? $"EXISTS (SELECT 1 FROM json_each(COALESCE(Tags,'[]')) WHERE json_each.value = {paramName})"
                    : $"EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Tags, '[]')) AS t WHERE t.value = {paramName})");
            }
        }

        var sql = baseSql;
        if (conditions.Count > 0)
        {
            sql += " AND " + string.Join(" AND ", conditions);
        }
        var orderColumn = ResolveOrderColumn(sortBy);
        var sortDescending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        // Ensure stable paging by always appending ComparisonRecordId as a tiebreaker.
        sql += sortDescending
            ? $" ORDER BY {orderColumn} DESC, ComparisonRecordId DESC"
            : $" ORDER BY {orderColumn} ASC, ComparisonRecordId ASC";
        sql += isSqlite
            ? " LIMIT @Limit OFFSET @Skip;"
            : " OFFSET @Skip ROWS FETCH NEXT @Limit ROWS ONLY;";

        var rows = await connection.QueryAsync<ComparisonRecord>(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<ComparisonRecord>> SearchByCursorAsync(
        string? comparisonType,
        string? leftRunId,
        string? rightRunId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? leftExportRecordId,
        string? rightExportRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        string? sortBy,
        string? sortDir,
        DateTime? cursorCreatedUtc,
        string? cursorComparisonRecordId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string baseSql = """
            SELECT *
            FROM ComparisonRecords
            WHERE 1 = 1
            """;

        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        var safeLimit = limit <= 0 ? 50 : Math.Min(limit, 500);
        parameters.Add("@Limit", safeLimit);

        if (!string.IsNullOrWhiteSpace(comparisonType))
        {
            conditions.Add("ComparisonType = @ComparisonType");
            parameters.Add("@ComparisonType", comparisonType);
        }

        if (!string.IsNullOrWhiteSpace(leftRunId))
        {
            conditions.Add("LeftRunId = @LeftRunId");
            parameters.Add("@LeftRunId", leftRunId);
        }

        if (!string.IsNullOrWhiteSpace(rightRunId))
        {
            conditions.Add("RightRunId = @RightRunId");
            parameters.Add("@RightRunId", rightRunId);
        }

        if (createdFromUtc is not null)
        {
            conditions.Add("CreatedUtc >= @CreatedFromUtc");
            parameters.Add("@CreatedFromUtc", createdFromUtc);
        }

        if (createdToUtc is not null)
        {
            conditions.Add("CreatedUtc <= @CreatedToUtc");
            parameters.Add("@CreatedToUtc", createdToUtc);
        }

        using var connection = _connectionFactory.CreateConnection();

        if (!string.IsNullOrWhiteSpace(leftExportRecordId))
        {
            conditions.Add("LeftExportRecordId = @LeftExportRecordId");
            parameters.Add("@LeftExportRecordId", leftExportRecordId);
        }

        if (!string.IsNullOrWhiteSpace(rightExportRecordId))
        {
            conditions.Add("RightExportRecordId = @RightExportRecordId");
            parameters.Add("@RightExportRecordId", rightExportRecordId);
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            conditions.Add("Label = @Label");
            parameters.Add("@Label", label);
        }

        var isSqlite = IsSqlite(connection);
        if (tags is { Count: > 0 })
        {
            for (var i = 0; i < tags.Count; i++)
            {
                var t = tags[i];
                if (string.IsNullOrWhiteSpace(t)) continue;
                var paramName = $"@Tag{i}";
                parameters.Add(paramName, t);
                conditions.Add(isSqlite
                    ? $"EXISTS (SELECT 1 FROM json_each(COALESCE(Tags,'[]')) WHERE json_each.value = {paramName})"
                    : $"EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Tags, '[]')) AS t WHERE t.value = {paramName})");
            }
        }

        var orderColumn = ResolveOrderColumn(sortBy);
        var sortDescending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        // Cursor paging: only supported for CreatedUtc ordering (plus ComparisonRecordId tiebreaker).
        if (!string.Equals(orderColumn, "CreatedUtc", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cursor paging currently supports sortBy=createdUtc only.");
        }

        if (cursorCreatedUtc is not null && !string.IsNullOrWhiteSpace(cursorComparisonRecordId))
        {
            parameters.Add("@CursorCreatedUtc", cursorCreatedUtc);
            parameters.Add("@CursorId", cursorComparisonRecordId);
            // For DESC: fetch items strictly "after" cursor in DESC order => older than cursor.
            // For ASC: fetch items strictly "after" cursor in ASC order => newer than cursor.
            conditions.Add(sortDescending
                ? "(CreatedUtc < @CursorCreatedUtc OR (CreatedUtc = @CursorCreatedUtc AND ComparisonRecordId < @CursorId))"
                : "(CreatedUtc > @CursorCreatedUtc OR (CreatedUtc = @CursorCreatedUtc AND ComparisonRecordId > @CursorId))");
        }

        var sql = baseSql;
        if (conditions.Count > 0)
        {
            sql += " AND " + string.Join(" AND ", conditions);
        }

        sql += sortDescending
            ? $" ORDER BY {orderColumn} DESC, ComparisonRecordId DESC"
            : $" ORDER BY {orderColumn} ASC, ComparisonRecordId ASC";

        sql += isSqlite
            ? " LIMIT @Limit;"
            : " OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY;";

        var rows = await connection.QueryAsync<ComparisonRecord>(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<bool> UpdateLabelAndTagsAsync(
        string comparisonRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE ComparisonRecords
            SET Label = @Label,
                Tags = @Tags
            WHERE ComparisonRecordId = @ComparisonRecordId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var tagsJson = tags == null || tags.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(tags);
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ComparisonRecordId = comparisonRecordId, Label = label ?? (object)DBNull.Value, Tags = tagsJson ?? (object)DBNull.Value },
            cancellationToken: cancellationToken));
        return rows > 0;
    }

    private static bool IsSqlite(IDbConnection connection) => connection is SqliteConnection;

    private static string ResolveOrderColumn(string? sortBy)
    {
        var v = (sortBy ?? "createdUtc").Trim().ToLowerInvariant();
        return v switch
        {
            "createdutc" => "CreatedUtc",
            "created" => "CreatedUtc",
            "type" => "ComparisonType",
            "comparisontype" => "ComparisonType",
            "label" => "Label",
            "leftrunid" => "LeftRunId",
            "rightrunid" => "RightRunId",
            _ => "CreatedUtc"
        };
    }
}
