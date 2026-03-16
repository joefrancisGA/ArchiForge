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
}
