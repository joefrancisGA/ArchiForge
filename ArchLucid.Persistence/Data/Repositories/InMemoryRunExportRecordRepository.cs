using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IRunExportRecordRepository"/> (JSON clone-on-read).
/// </summary>
public sealed class InMemoryRunExportRecordRepository : IRunExportRecordRepository
{
    private readonly Dictionary<string, RunExportRecord> _byExportId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<RunExportRecord>> _byRunId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(RunExportRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(record.ExportRecordId))
        
            throw new ArgumentException("ExportRecordId is required.", nameof(record));
        

        RunExportRecord stored = Clone(record);

        lock (_gate)
        {
            _byExportId[record.ExportRecordId] = stored;

            if (!_byRunId.TryGetValue(record.RunId, out List<RunExportRecord>? list))
            {
                list = [];
                _byRunId[record.RunId] = list;
            }

            list.Add(stored);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RunExportRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byRunId.TryGetValue(runId, out List<RunExportRecord>? list))
            
                return Task.FromResult<IReadOnlyList<RunExportRecord>>([]);
            

            List<RunExportRecord> ordered = list
                .OrderByDescending(r => r.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<RunExportRecord>>(ordered);
        }
    }

    /// <inheritdoc />
    public Task<RunExportRecord?> GetByIdAsync(
        string exportRecordId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        
            return Task.FromResult(
                _byExportId.TryGetValue(exportRecordId, out RunExportRecord? r) ? Clone(r) : null);
        
    }

    private static RunExportRecord Clone(RunExportRecord source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        RunExportRecord? copy = JsonSerializer.Deserialize<RunExportRecord>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null RunExportRecord.");
    }
}
