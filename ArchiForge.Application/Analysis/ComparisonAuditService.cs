using System.Text.Json;

using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// Persists comparison results as immutable <see cref="ComparisonRecord"/> entries for audit and replay.
/// </summary>
public sealed class ComparisonAuditService(IComparisonRecordRepository repository) : IComparisonAuditService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <inheritdoc />
    public async Task<string> RecordEndToEndAsync(
        EndToEndReplayComparisonReport report,
        string summaryMarkdown,
        CancellationToken cancellationToken = default)
    {
        ComparisonRecord record = new ComparisonRecord
        {
            ComparisonRecordId = Guid.NewGuid().ToString("N"),
            ComparisonType = ComparisonTypes.EndToEndReplay,
            LeftRunId = report.LeftRunId,
            RightRunId = report.RightRunId,
            Format = ComparisonTypes.FormatJsonMarkdown,
            SummaryMarkdown = summaryMarkdown,
            PayloadJson = JsonSerializer.Serialize(report, _jsonOptions),
            Notes = "Persisted end-to-end replay comparison.",
            CreatedUtc = DateTime.UtcNow
        };

        await repository.CreateAsync(record, cancellationToken);
        return record.ComparisonRecordId;
    }

    /// <inheritdoc />
    public async Task<string> RecordExportDiffAsync(
        ExportRecordDiffResult diff,
        string summaryMarkdown,
        CancellationToken cancellationToken = default)
    {
        ComparisonRecord record = new ComparisonRecord
        {
            ComparisonRecordId = Guid.NewGuid().ToString("N"),
            ComparisonType = ComparisonTypes.ExportRecordDiff,
            LeftRunId = diff.LeftRunId,
            RightRunId = diff.RightRunId,
            LeftExportRecordId = diff.LeftExportRecordId,
            RightExportRecordId = diff.RightExportRecordId,
            Format = ComparisonTypes.FormatJsonMarkdown,
            SummaryMarkdown = summaryMarkdown,
            PayloadJson = JsonSerializer.Serialize(diff, _jsonOptions),
            Notes = "Persisted export record diff.",
            CreatedUtc = DateTime.UtcNow
        };

        await repository.CreateAsync(record, cancellationToken);
        return record.ComparisonRecordId;
    }

    /// <inheritdoc />
    public async Task<string> RecordReplayOfAsync(
        ComparisonRecord sourceRecord,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        ComparisonRecord record = new ComparisonRecord
        {
            ComparisonRecordId = Guid.NewGuid().ToString("N"),
            ComparisonType = sourceRecord.ComparisonType,
            LeftRunId = sourceRecord.LeftRunId,
            RightRunId = sourceRecord.RightRunId,
            LeftExportRecordId = sourceRecord.LeftExportRecordId,
            RightExportRecordId = sourceRecord.RightExportRecordId,
            LeftManifestVersion = sourceRecord.LeftManifestVersion,
            RightManifestVersion = sourceRecord.RightManifestVersion,
            Format = sourceRecord.Format,
            SummaryMarkdown = sourceRecord.SummaryMarkdown,
            PayloadJson = sourceRecord.PayloadJson,
            Notes = notes ?? $"Replay of comparison record {sourceRecord.ComparisonRecordId}.",
            CreatedUtc = DateTime.UtcNow
        };

        await repository.CreateAsync(record, cancellationToken);
        return record.ComparisonRecordId;
    }
}

