using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IComparisonRecordRepository"/> for tests and in-memory hosts.
/// </summary>
/// <remarks>Filter and sort semantics mirror <see cref="ComparisonRecordRepository"/> for contract tests.</remarks>
public sealed class InMemoryComparisonRecordRepository : IComparisonRecordRepository
{
    private const int MaxEntries = 5_000;

    private readonly List<ComparisonRecord> _items = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(ComparisonRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_items.Count >= MaxEntries)
                _items.RemoveAt(0);

            _items.Add(Clone(record));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ComparisonRecord?> GetByIdAsync(string comparisonRecordId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            ComparisonRecord? row = _items.FirstOrDefault(r =>
                string.Equals(r.ComparisonRecordId, comparisonRecordId, StringComparison.Ordinal));

            return Task.FromResult(row is null ? null : Clone(row));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComparisonRecord>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<ComparisonRecord> list = _items
                .Where(r =>
                    string.Equals(r.LeftRunId, runId, StringComparison.Ordinal) ||
                    string.Equals(r.RightRunId, runId, StringComparison.Ordinal))
                .OrderByDescending(r => r.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<ComparisonRecord>>(list);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComparisonRecord>> GetByExportRecordIdAsync(
        string exportRecordId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<ComparisonRecord> list = _items
                .Where(r =>
                    string.Equals(r.LeftExportRecordId, exportRecordId, StringComparison.Ordinal) ||
                    string.Equals(r.RightExportRecordId, exportRecordId, StringComparison.Ordinal))
                .OrderByDescending(r => r.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<ComparisonRecord>>(list);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComparisonRecord>> SearchAsync(
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
        cancellationToken.ThrowIfCancellationRequested();

        int safeLimit = limit <= 0 ? 50 : Math.Min(limit, 500);
        int safeSkip = skip < 0 ? 0 : skip;

        lock (_gate)
        {
            IEnumerable<ComparisonRecord> query = _items.Select(Clone);
            query = ApplyFilters(
                query,
                comparisonType,
                leftRunId,
                rightRunId,
                createdFromUtc,
                createdToUtc,
                leftExportRecordId,
                rightExportRecordId,
                label,
                tags);
            query = ApplyOrdering(query, sortBy, sortDir);
            List<ComparisonRecord> page = query.Skip(safeSkip).Take(safeLimit).ToList();

            return Task.FromResult<IReadOnlyList<ComparisonRecord>>(page);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComparisonRecord>> SearchByCursorAsync(
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
        cancellationToken.ThrowIfCancellationRequested();

        string orderColumn = ResolveOrderColumn(sortBy);

        if (!string.Equals(orderColumn, "CreatedUtc", StringComparison.OrdinalIgnoreCase))
        
            throw new InvalidOperationException("Cursor paging currently supports sortBy=createdUtc only.");
        

        int safeLimit = limit <= 0 ? 50 : Math.Min(limit, 500);
        bool sortDescending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        lock (_gate)
        {
            IEnumerable<ComparisonRecord> query = _items.Select(Clone);
            query = ApplyFilters(
                query,
                comparisonType,
                leftRunId,
                rightRunId,
                createdFromUtc,
                createdToUtc,
                leftExportRecordId,
                rightExportRecordId,
                label,
                tags);

            if (cursorCreatedUtc is not null && !string.IsNullOrWhiteSpace(cursorComparisonRecordId))
            
                query = query.Where(r =>
                    sortDescending
                        ? r.CreatedUtc < cursorCreatedUtc.Value ||
                          (r.CreatedUtc == cursorCreatedUtc.Value &&
                           string.Compare(r.ComparisonRecordId, cursorComparisonRecordId, StringComparison.Ordinal) < 0)
                        : r.CreatedUtc > cursorCreatedUtc.Value ||
                          (r.CreatedUtc == cursorCreatedUtc.Value &&
                           string.Compare(r.ComparisonRecordId, cursorComparisonRecordId, StringComparison.Ordinal) > 0));
            

            query = ApplyOrdering(query, sortBy, sortDir);
            List<ComparisonRecord> page = query.Take(safeLimit).ToList();

            return Task.FromResult<IReadOnlyList<ComparisonRecord>>(page);
        }
    }

    /// <inheritdoc />
    public Task<bool> UpdateLabelAndTagsAsync(
        string comparisonRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonRecordId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            int i = _items.FindIndex(r =>
                string.Equals(r.ComparisonRecordId, comparisonRecordId, StringComparison.Ordinal));

            if (i < 0)
                return Task.FromResult(false);

            if (label is not null)
                _items[i].Label = label;

            if (tags is not null)
                _items[i].Tags = [..tags];

            return Task.FromResult(true);
        }
    }

    private static IEnumerable<ComparisonRecord> ApplyFilters(
        IEnumerable<ComparisonRecord> source,
        string? comparisonType,
        string? leftRunId,
        string? rightRunId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? leftExportRecordId,
        string? rightExportRecordId,
        string? label,
        IReadOnlyList<string>? tags)
    {
        IEnumerable<ComparisonRecord> q = source;

        if (!string.IsNullOrWhiteSpace(comparisonType))
            q = q.Where(r => string.Equals(r.ComparisonType, comparisonType, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(leftRunId))
            q = q.Where(r => string.Equals(r.LeftRunId, leftRunId, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(rightRunId))
            q = q.Where(r => string.Equals(r.RightRunId, rightRunId, StringComparison.Ordinal));

        if (createdFromUtc is not null)
            q = q.Where(r => r.CreatedUtc >= createdFromUtc.Value);

        if (createdToUtc is not null)
            q = q.Where(r => r.CreatedUtc <= createdToUtc.Value);

        if (!string.IsNullOrWhiteSpace(leftExportRecordId))
            q = q.Where(r => string.Equals(r.LeftExportRecordId, leftExportRecordId, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(rightExportRecordId))
            q = q.Where(r => string.Equals(r.RightExportRecordId, rightExportRecordId, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(label))
            q = q.Where(r => string.Equals(r.Label, label, StringComparison.Ordinal));

        if (tags is { Count: > 0 })
        
            foreach (string t in tags)
            {
                if (string.IsNullOrWhiteSpace(t))
                    continue;

                string needle = t;

                q = q.Where(r => r.Tags.Any(x => string.Equals(x, needle, StringComparison.Ordinal)));
            }
        

        return q;
    }

    private static IEnumerable<ComparisonRecord> ApplyOrdering(
        IEnumerable<ComparisonRecord> source,
        string? sortBy,
        string? sortDir)
    {
        string col = ResolveOrderColumn(sortBy);
        bool desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        IOrderedEnumerable<ComparisonRecord> ordered = col.ToLowerInvariant() switch
        {
            "comparisontype" or "type" => desc
                ? source.OrderByDescending(r => r.ComparisonType).ThenByDescending(r => r.ComparisonRecordId)
                : source.OrderBy(r => r.ComparisonType).ThenBy(r => r.ComparisonRecordId),
            "label" => desc
                ? source.OrderByDescending(r => r.Label).ThenByDescending(r => r.ComparisonRecordId)
                : source.OrderBy(r => r.Label).ThenBy(r => r.ComparisonRecordId),
            "leftrunid" => desc
                ? source.OrderByDescending(r => r.LeftRunId).ThenByDescending(r => r.ComparisonRecordId)
                : source.OrderBy(r => r.LeftRunId).ThenBy(r => r.ComparisonRecordId),
            "rightrunid" => desc
                ? source.OrderByDescending(r => r.RightRunId).ThenByDescending(r => r.ComparisonRecordId)
                : source.OrderBy(r => r.RightRunId).ThenBy(r => r.ComparisonRecordId),
            _ => desc
                ? source.OrderByDescending(r => r.CreatedUtc).ThenByDescending(r => r.ComparisonRecordId)
                : source.OrderBy(r => r.CreatedUtc).ThenBy(r => r.ComparisonRecordId)
        };

        return ordered;
    }

    private static string ResolveOrderColumn(string? sortBy)
    {
        string v = (sortBy ?? "createdUtc").Trim().ToLowerInvariant();

        return v switch
        {
            "createdutc" or "created" => "CreatedUtc",
            "type" or "comparisontype" => "ComparisonType",
            "label" => "Label",
            "leftrunid" => "LeftRunId",
            "rightrunid" => "RightRunId",
            _ => "CreatedUtc"
        };
    }

    /// <summary>
    /// Integration test support: overwrites <see cref="ComparisonRecord.PayloadJson"/> for an existing record in this in-memory store.
    /// </summary>
    internal void ReplacePayloadJsonForIntegrationTest(string comparisonRecordId, string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonRecordId);
        ArgumentNullException.ThrowIfNull(payloadJson);

        lock (_gate)
        {
            ComparisonRecord? row = _items.FirstOrDefault(r =>
                string.Equals(r.ComparisonRecordId, comparisonRecordId, StringComparison.Ordinal));

            if (row is null)
                throw new InvalidOperationException($"Comparison record '{comparisonRecordId}' was not found.");

            row.PayloadJson = payloadJson;
        }
    }

    private static ComparisonRecord Clone(ComparisonRecord r)
    {
        return new ComparisonRecord
        {
            ComparisonRecordId = r.ComparisonRecordId,
            ComparisonType = r.ComparisonType,
            LeftRunId = r.LeftRunId,
            RightRunId = r.RightRunId,
            LeftManifestVersion = r.LeftManifestVersion,
            RightManifestVersion = r.RightManifestVersion,
            LeftExportRecordId = r.LeftExportRecordId,
            RightExportRecordId = r.RightExportRecordId,
            Format = r.Format,
            SummaryMarkdown = r.SummaryMarkdown,
            PayloadJson = r.PayloadJson,
            Notes = r.Notes,
            CreatedUtc = r.CreatedUtc,
            Label = r.Label,
            Tags = r.Tags is null ? [] : [..r.Tags]
        };
    }
}
