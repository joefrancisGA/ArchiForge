using System.Text.Json;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class RunExportRecordRepository : IRunExportRecordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RunExportRecordRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(
        RunExportRecord record,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO RunExportRecords
            (
                ExportRecordId,
                RunId,
                ExportType,
                Format,
                FileName,
                TemplateProfile,
                TemplateProfileDisplayName,
                WasAutoSelected,
                ResolutionReason,
                ManifestVersion,
                Notes,
                AnalysisRequestJson,
                IncludedEvidence,
                IncludedExecutionTraces,
                IncludedManifest,
                IncludedDiagram,
                IncludedSummary,
                IncludedDeterminismCheck,
                DeterminismIterations,
                IncludedManifestCompare,
                CompareManifestVersion,
                IncludedAgentResultCompare,
                CompareRunId,
                RecordJson,
                CreatedUtc
            )
            VALUES
            (
                @ExportRecordId,
                @RunId,
                @ExportType,
                @Format,
                @FileName,
                @TemplateProfile,
                @TemplateProfileDisplayName,
                @WasAutoSelected,
                @ResolutionReason,
                @ManifestVersion,
                @Notes,
                @AnalysisRequestJson,
                @IncludedEvidence,
                @IncludedExecutionTraces,
                @IncludedManifest,
                @IncludedDiagram,
                @IncludedSummary,
                @IncludedDeterminismCheck,
                @DeterminismIterations,
                @IncludedManifestCompare,
                @CompareManifestVersion,
                @IncludedAgentResultCompare,
                @CompareRunId,
                @RecordJson,
                @CreatedUtc
            );
            """;

        var json = JsonSerializer.Serialize(record, ContractJson.Default);

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                record.ExportRecordId,
                record.RunId,
                record.ExportType,
                record.Format,
                record.FileName,
                record.TemplateProfile,
                record.TemplateProfileDisplayName,
                record.WasAutoSelected,
                record.ResolutionReason,
                record.ManifestVersion,
                record.Notes,
                record.AnalysisRequestJson,
                record.IncludedEvidence,
                record.IncludedExecutionTraces,
                record.IncludedManifest,
                record.IncludedDiagram,
                record.IncludedSummary,
                record.IncludedDeterminismCheck,
                record.DeterminismIterations,
                record.IncludedManifestCompare,
                record.CompareManifestVersion,
                record.IncludedAgentResultCompare,
                record.CompareRunId,
                RecordJson = json,
                record.CreatedUtc
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<RunExportRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT RecordJson
            FROM RunExportRecords
            WHERE RunId = @RunId
            ORDER BY CreatedUtc DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return rows
            .Select(json => JsonSerializer.Deserialize<RunExportRecord>(json, ContractJson.Default))
            .Where(x => x is not null)
            .Cast<RunExportRecord>()
            .ToList();
    }

    public async Task<RunExportRecord?> GetByIdAsync(
        string exportRecordId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT RecordJson
            FROM RunExportRecords
            WHERE ExportRecordId = @ExportRecordId;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new { ExportRecordId = exportRecordId },
            cancellationToken: cancellationToken));

        return json is null
            ? null
            : JsonSerializer.Deserialize<RunExportRecord>(json, ContractJson.Default);
    }
}

