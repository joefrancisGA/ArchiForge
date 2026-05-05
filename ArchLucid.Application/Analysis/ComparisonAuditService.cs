using System.Text.Json;

using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Application.Analysis;

/// <summary>
///     Persists comparison results as immutable <see cref="ComparisonRecord" /> entries for audit and replay.
/// </summary>
public sealed class ComparisonAuditService(IComparisonRecordRepository repository, IAuditService auditService)
    : IComparisonAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IComparisonRecordRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    /// <inheritdoc />
    public async Task<string> RecordEndToEndAsync(
        EndToEndReplayComparisonReport report,
        string summaryMarkdown,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        ComparisonRecord record = new()
        {
            ComparisonRecordId = Guid.NewGuid().ToString("N"),
            ComparisonType = ComparisonTypes.EndToEndReplay,
            LeftRunId = report.LeftRunId,
            RightRunId = report.RightRunId,
            Format = ComparisonTypes.FormatJsonMarkdown,
            SummaryMarkdown = summaryMarkdown,
            PayloadJson = JsonSerializer.Serialize(report, JsonOptions),
            Notes = "Persisted end-to-end replay comparison.",
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(record, cancellationToken);

        DateTime occurredUtc = DateTime.UtcNow;

        await _auditService.LogAsync(
            new AuditEvent
            {
                OccurredUtc = occurredUtc,
                EventType = AuditEventTypes.EndToEndComparisonPersisted,
                RunId = TryParseRunGuid(record.LeftRunId) ?? TryParseRunGuid(record.RightRunId),
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        comparisonRecordId = record.ComparisonRecordId,
                        leftRunId = record.LeftRunId,
                        rightRunId = record.RightRunId,
                        comparisonType = record.ComparisonType
                    },
                    AuditJsonSerializationOptions.Instance)
            },
            cancellationToken);

        return record.ComparisonRecordId;
    }

    /// <inheritdoc />
    public async Task<string> RecordExportDiffAsync(
        ExportRecordDiffResult diff,
        string summaryMarkdown,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diff);

        ComparisonRecord record = new()
        {
            ComparisonRecordId = Guid.NewGuid().ToString("N"),
            ComparisonType = ComparisonTypes.ExportRecordDiff,
            LeftRunId = diff.LeftRunId,
            RightRunId = diff.RightRunId,
            LeftExportRecordId = diff.LeftExportRecordId,
            RightExportRecordId = diff.RightExportRecordId,
            Format = ComparisonTypes.FormatJsonMarkdown,
            SummaryMarkdown = summaryMarkdown,
            PayloadJson = JsonSerializer.Serialize(diff, JsonOptions),
            Notes = "Persisted export record diff.",
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(record, cancellationToken);

        // Durable `ComparisonSummaryPersisted` is emitted by `ExportsController` after this call; avoid duplicate rows.

        return record.ComparisonRecordId;
    }

    /// <inheritdoc />
    public async Task<string> RecordReplayOfAsync(
        ComparisonRecord sourceRecord,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceRecord);

        ComparisonRecord record = new()
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

        await _repository.CreateAsync(record, cancellationToken);

        DateTime occurredUtc = DateTime.UtcNow;

        await _auditService.LogAsync(
            new AuditEvent
            {
                OccurredUtc = occurredUtc,
                EventType = AuditEventTypes.ComparisonReplayPersisted,
                RunId = TryParseRunGuid(record.LeftRunId) ?? TryParseRunGuid(record.RightRunId),
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        comparisonRecordId = record.ComparisonRecordId,
                        sourceComparisonRecordId = sourceRecord.ComparisonRecordId,
                        leftRunId = record.LeftRunId,
                        rightRunId = record.RightRunId,
                        comparisonType = record.ComparisonType
                    },
                    AuditJsonSerializationOptions.Instance)
            },
            cancellationToken);

        return record.ComparisonRecordId;
    }

    private static Guid? TryParseRunGuid(string? runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return null;

        if (Guid.TryParseExact(runId, "N", out Guid guid))
            return guid;

        if (Guid.TryParse(runId, out guid))
            return guid;

        return null;
    }
}
