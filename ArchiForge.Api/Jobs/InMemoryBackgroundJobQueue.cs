using System.Collections.Concurrent;
using System.Threading.Channels;

using JetBrains.Annotations;

namespace ArchiForge.Api.Jobs;

public sealed class InMemoryBackgroundJobQueue(ILogger<InMemoryBackgroundJobQueue> logger) : BackgroundService, IBackgroundJobQueue
{
    private sealed record WorkItem(
        string JobId,
        [UsedImplicitly] string? FileNameHint,
        string? ContentTypeHint,
        Func<CancellationToken, Task<BackgroundJobFile>> Work);

    /// <summary>Maximum number of terminal jobs (Succeeded/Failed) retained in memory before evicting the oldest.</summary>
    private const int MaxRetainedTerminalJobs = 200;

    /// <summary>
    /// Maximum number of jobs that may wait in the channel before <see cref="Enqueue"/> throws.
    /// Prevents unbounded memory growth under sustained load; callers should back off and retry on failure.
    /// </summary>
    private const int MaxPendingJobs = 500;

    private readonly Channel<WorkItem> _queue = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(MaxPendingJobs)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });

    private readonly ConcurrentDictionary<string, BackgroundJobInfo> _info = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, BackgroundJobFile> _files = new(StringComparer.Ordinal);

    public string Enqueue(string? fileNameHint, string? contentTypeHint, Func<CancellationToken, Task<BackgroundJobFile>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        string id = Guid.NewGuid().ToString("n");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        _info[id] = new BackgroundJobInfo(
            JobId: id,
            State: BackgroundJobState.Pending,
            CreatedUtc: now,
            StartedUtc: null,
            CompletedUtc: null,
            Error: null,
            FileName: fileNameHint,
            ContentType: contentTypeHint);

        if (_queue.Writer.TryWrite(new WorkItem(id, fileNameHint, contentTypeHint, work))) return id;
        
        _info.TryRemove(id, out _);
        
        throw new InvalidOperationException(
            $"The background job queue is at capacity ({MaxPendingJobs} pending jobs). Try again later.");
    }

    public BackgroundJobInfo? GetInfo(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return null;
        return _info.TryGetValue(jobId, out BackgroundJobInfo? info) ? info : null;
    }

    public BackgroundJobFile? GetFile(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return null;
        return _files.TryGetValue(jobId, out BackgroundJobFile? file) ? file : null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (WorkItem item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_info.TryGetValue(item.JobId, out BackgroundJobInfo? current))
                continue;

            _info[item.JobId] = current with
            {
                State = BackgroundJobState.Running,
                StartedUtc = DateTimeOffset.UtcNow
            };

            try
            {
                BackgroundJobFile file = await item.Work(stoppingToken);
                _files[item.JobId] = file;

                BackgroundJobInfo done = _info[item.JobId];
                _info[item.JobId] = done with
                {
                    State = BackgroundJobState.Succeeded,
                    CompletedUtc = DateTimeOffset.UtcNow,
                    Error = null,
                    FileName = file.FileName,
                    ContentType = file.ContentType
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background job {JobId} failed.", item.JobId);

                BackgroundJobInfo failed = _info[item.JobId];
                _info[item.JobId] = failed with
                {
                    State = BackgroundJobState.Failed,
                    CompletedUtc = DateTimeOffset.UtcNow,
                    Error = ex.Message
                };
            }

            EvictOldTerminalJobs();
        }
    }

    /// <summary>
    /// Removes the oldest terminal-state jobs when the retained count exceeds <see cref="MaxRetainedTerminalJobs"/>.
    /// This prevents unbounded memory growth for long-running server instances.
    /// </summary>
    private void EvictOldTerminalJobs()
    {
        List<BackgroundJobInfo> terminal = _info.Values
            .Where(j => j.State is BackgroundJobState.Succeeded or BackgroundJobState.Failed)
            .OrderBy(j => j.CompletedUtc)
            .ToList();

        if (terminal.Count <= MaxRetainedTerminalJobs)
            return;

        foreach (BackgroundJobInfo old in terminal.Take(terminal.Count - MaxRetainedTerminalJobs))
        {
            _info.TryRemove(old.JobId, out _);
            _files.TryRemove(old.JobId, out _);
        }
    }
}

