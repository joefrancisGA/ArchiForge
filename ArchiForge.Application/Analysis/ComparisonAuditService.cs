using System.Text.Json;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

public sealed class ComparisonAuditService : IComparisonAuditService
{
    private readonly IComparisonRecordRepository _repository;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public ComparisonAuditService(IComparisonRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> RecordEndToEndAsync(
        EndToEndReplayComparisonReport report,
        string summaryMarkdown,
        CancellationToken cancellationToken = default)
    {
        var record = new ComparisonRecord
        {
            ComparisonRecordId = Guid.NewGuid().ToString("N"),
            ComparisonType = "end-to-end-replay",
            LeftRunId = report.LeftRunId,
            RightRunId = report.RightRunId,
            Format = "json+markdown",
            SummaryMarkdown = summaryMarkdown,
            PayloadJson = JsonSerializer.Serialize(report, _jsonOptions),
            Notes = "Persisted end-to-end replay comparison.",
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(record, cancellationToken);
        return record.ComparisonRecordId;
    }

    public async Task<string> RecordExportDiffAsync(
        ExportRecordDiffResult diff,
        string summaryMarkdown,
        CancellationToken cancellationToken = default)
    {
        var record = new ComparisonRecord
        {
            ComparisonRecordId = Guid.NewGuid().ToString("N"),
            ComparisonType = "export-record-diff",
            LeftRunId = diff.LeftRunId,
            RightRunId = diff.RightRunId,
            LeftExportRecordId = diff.LeftExportRecordId,
            RightExportRecordId = diff.RightExportRecordId,
            Format = "json+markdown",
            SummaryMarkdown = summaryMarkdown,
            PayloadJson = JsonSerializer.Serialize(diff, _jsonOptions),
            Notes = "Persisted export record diff.",
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(record, cancellationToken);
        return record.ComparisonRecordId;
    }
}

