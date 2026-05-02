using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Thread-safe in-memory <see cref="IAgentExecutionTraceRepository" /> for tests (JSON clone-on-read).
/// </summary>
public sealed class InMemoryAgentExecutionTraceRepository : IAgentExecutionTraceRepository
{
    private readonly Lock _gate = new();
    private readonly List<AgentExecutionTrace> _items = [];

    /// <inheritdoc />
    public Task CreateAsync(AgentExecutionTrace trace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trace);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)

            _items.Add(Clone(trace));


        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PatchBlobStorageFieldsAsync(
        string traceId,
        string? fullSystemPromptBlobKey,
        string? fullUserPromptBlobKey,
        string? fullResponseBlobKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            int i = _items.FindIndex(t => string.Equals(t.TraceId, traceId, StringComparison.Ordinal));
            if (i < 0)
                return Task.CompletedTask;


            AgentExecutionTrace t = Clone(_items[i]);

            if (fullSystemPromptBlobKey is not null)

                t.FullSystemPromptBlobKey = fullSystemPromptBlobKey;


            if (fullUserPromptBlobKey is not null)

                t.FullUserPromptBlobKey = fullUserPromptBlobKey;


            if (fullResponseBlobKey is not null)

                t.FullResponseBlobKey = fullResponseBlobKey;


            _items[i] = t;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PatchBlobUploadFailedAsync(
        string traceId,
        bool failed,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            int i = _items.FindIndex(t => string.Equals(t.TraceId, traceId, StringComparison.Ordinal));

            if (i >= 0)
            {
                AgentExecutionTrace t = Clone(_items[i]);
                t.BlobUploadFailed = failed;
                _items[i] = t;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PatchInlinePromptFallbackAsync(
        string traceId,
        string? fullSystemPromptInline,
        string? fullUserPromptInline,
        string? fullResponseInline,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            int i = _items.FindIndex(t => string.Equals(t.TraceId, traceId, StringComparison.Ordinal));

            if (i < 0)
                return Task.CompletedTask;


            AgentExecutionTrace t = Clone(_items[i]);

            if (fullSystemPromptInline is not null)

                t.FullSystemPromptInline = fullSystemPromptInline;


            if (fullUserPromptInline is not null)

                t.FullUserPromptInline = fullUserPromptInline;


            if (fullResponseInline is not null)

                t.FullResponseInline = fullResponseInline;


            _items[i] = t;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PatchInlineFallbackFailedAsync(
        string traceId,
        bool failed,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            int i = _items.FindIndex(t => string.Equals(t.TraceId, traceId, StringComparison.Ordinal));

            if (i >= 0)
            {
                AgentExecutionTrace t = Clone(_items[i]);
                t.InlineFallbackFailed = failed ? true : null;
                _items[i] = t;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PatchQualityWarningAsync(
        string traceId,
        bool qualityWarning,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            int i = _items.FindIndex(t => string.Equals(t.TraceId, traceId, StringComparison.Ordinal));

            if (i < 0)
                return Task.CompletedTask;


            AgentExecutionTrace t = Clone(_items[i]);
            t.QualityWarning = qualityWarning;
            _items[i] = t;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AgentExecutionTrace?> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            AgentExecutionTrace? found = _items.FirstOrDefault(t =>
                string.Equals(t.TraceId, traceId, StringComparison.Ordinal));

            return Task.FromResult(found is null ? null : Clone(found));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentExecutionTrace>> GetByRunIdAsync(string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<AgentExecutionTrace> list = _items
                .Where(t => string.Equals(t.RunId, runId, StringComparison.Ordinal))
                .OrderBy(t => t.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<AgentExecutionTrace>>(list);
        }
    }

    /// <inheritdoc />
    public Task<(IReadOnlyList<AgentExecutionTrace> Traces, int TotalCount)> GetPagedByRunIdAsync(
        string runId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<AgentExecutionTrace> ordered = _items
                .Where(t => string.Equals(t.RunId, runId, StringComparison.Ordinal))
                .OrderBy(t => t.CreatedUtc)
                .ToList();

            int total = ordered.Count;
            int clampedOffset = Math.Max(0, offset);
            int clampedLimit = Math.Clamp(limit, 1, 500);
            List<AgentExecutionTrace> page = ordered
                .Skip(clampedOffset)
                .Take(clampedLimit)
                .Select(Clone)
                .ToList();

            return Task.FromResult<(IReadOnlyList<AgentExecutionTrace>, int)>((page, total));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(string taskId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<AgentExecutionTrace> list = _items
                .Where(t => string.Equals(t.TaskId, taskId, StringComparison.Ordinal))
                .OrderBy(t => t.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<AgentExecutionTrace>>(list);
        }
    }

    private static AgentExecutionTrace Clone(AgentExecutionTrace source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        AgentExecutionTrace? copy = JsonSerializer.Deserialize<AgentExecutionTrace>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null AgentExecutionTrace.");
    }
}
