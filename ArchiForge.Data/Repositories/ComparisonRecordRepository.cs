using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class ComparisonRecordRepository : IComparisonRecordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ComparisonRecordRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
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
                CreatedUtc
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
                @CreatedUtc
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
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string baseSql = """
            SELECT TOP (@Limit) *
            FROM ComparisonRecords
            WHERE 1 = 1
            """;

        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("@Limit", limit <= 0 ? 50 : Math.Min(limit, 500));

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

        var sql = baseSql;
        if (conditions.Count > 0)
        {
            sql += " AND " + string.Join(" AND ", conditions);
        }
        sql += " ORDER BY CreatedUtc DESC;";

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<ComparisonRecord>(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
