using System.Data;
using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

/// <summary>
/// Dapper-backed persistence for <see cref="IRunExportRecordRepository"/>; persists and retrieves export records from the <c>RunExportRecords</c> table.
/// </summary>
public sealed class RunExportRecordRepository(IDbConnectionFactory connectionFactory) : IRunExportRecordRepository
{
    public async Task CreateAsync(
        RunExportRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

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

        string json = JsonSerializer.Serialize(record, ContractJson.Default);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

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
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RunExportRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        string sql = $"""
            SELECT RecordJson
            FROM RunExportRecords
            WHERE RunId = @RunId
            ORDER BY CreatedUtc DESC
            {SqlPagingSyntax.FirstRowsOnly(500)};
            """;

        IEnumerable<string> rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId
            },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        List<RunExportRecord> records = [];
        foreach (string json in rows)
        {
            RunExportRecord? record;
            try
            {
                record = JsonSerializer.Deserialize<RunExportRecord>(json, ContractJson.Default);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"A RunExportRecord for run '{runId}' could not be deserialized. " +
                    "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
            }

            if (record is null)
            {
                throw new InvalidOperationException(
                    $"A RunExportRecord row for run '{runId}' deserialized to null. " +
                    "The stored JSON may be empty or corrupt.");
            }

            records.Add(record);
        }

        return records;
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

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new
            {
                ExportRecordId = exportRecordId
            },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (json is null)
            return null;

        RunExportRecord? record;
        try
        {
            record = JsonSerializer.Deserialize<RunExportRecord>(json, ContractJson.Default);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"RunExportRecord JSON for '{exportRecordId}' could not be deserialized. " +
                "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
        }

        return record
            ?? throw new InvalidOperationException(
                $"RunExportRecord JSON for '{exportRecordId}' deserialized to null. " +
                "The stored JSON may be empty or corrupt.");
    }
}

