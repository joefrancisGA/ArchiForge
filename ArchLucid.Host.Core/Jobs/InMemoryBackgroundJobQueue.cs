using System.Collections.Concurrent;
using System.Threading.Channels;

using ArchLucid.Application.Jobs;
using ArchLucid.Core.Diagnostics;

using JetBrains.Annotations;

namespace ArchLucid.Host.Core.Jobs;

public sealed class InMemoryBackgroundJobQueue(
    ILogger<InMemoryBackgroundJobQueue> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService, IBackgroundJobQueue
{
    private sealed record WorkItem(string JobId, BackgroundJobWorkUnit WorkUnit, [UsedImplicitly] int MaxRetries);

    private readonly SemaphoreSlim _pendingJobs = new(
        InMemoryBackgroundJobQueueLimits.MaxPendingJobs,
        InMemoryBackgroundJobQueueLimits.MaxPendingJobs);

    private readonly Channel<WorkItem> _queue = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    private readonly ConcurrentDictionary<string, BackgroundJobInfo> _info = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, BackgroundJobFile> _files = new(StringComparer.Ordinal);

    public async Task<string> EnqueueAsync(
        BackgroundJobWorkUnit workUnit,
        int maxRetries = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workUnit);

        string id = Guid.NewGuid().ToString("n");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int safeMaxRetries = Math.Clamp(maxRetries, 0, 10);

        _info[id] = new BackgroundJobInfo(
            JobId: id,
            State: BackgroundJobState.Pending,
            CreatedUtc: now,
            StartedUtc: null,
            CompletedUtc: null,
            Error: null,
            FileName: null,
            ContentType: null,
            RetryCount: 0,
            MaxRetries: safeMaxRetries);

        if (!await _pendingJobs.WaitAsync(0, cancellationToken))
        {
            _info.TryRemove(id, out _);

            throw new InvalidOperationException(
                $"The background job queue is at capacity ({InMemoryBackgroundJobQueueLimits.MaxPendingJobs} pending jobs). Try again later.");
        }

        if (_queue.Writer.TryWrite(new WorkItem(id, workUnit, safeMaxRetries)))
            return id;

        _pendingJobs.Release();
        _info.TryRemove(id, out _);

        throw new InvalidOperationException("The background job queue writer is not accepting jobs.");
    }

    public Task<BackgroundJobInfo?> GetInfoAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(jobId)
            ? Task.FromResult<BackgroundJobInfo?>(null)
            : Task.FromResult(_info.TryGetValue(jobId, out BackgroundJobInfo? info) ? info : null);
    }

    public Task<BackgroundJobFile?> GetFileAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(jobId)
            ? Task.FromResult<BackgroundJobFile?>(null)
            : Task.FromResult(_files.TryGetValue(jobId, out BackgroundJobFile? file) ? file : null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (WorkItem item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            _pendingJobs.Release();

            if (!_info.TryGetValue(item.JobId, out BackgroundJobInfo? current))
                continue;

            _info[item.JobId] = current with { State = BackgroundJobState.Running, StartedUtc = current.StartedUtc ?? DateTimeOffset.UtcNow };

            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                IBackgroundJobWorkUnitExecutor executor = scope.ServiceProvider.GetRequiredService<IBackgroundJobWorkUnitExecutor>();

                BackgroundJobFile file = await executor.ExecuteAsync(item.WorkUnit, stoppingToken);
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
                BackgroundJobInfo failed = _info[item.JobId];
                int nextRetry = failed.RetryCount + 1;

                if (nextRetry <= failed.MaxRetries)
                {
                    logger.LogWarning(
                        ex,
                        "Background job {JobId} failed (attempt {Attempt}/{Max}); scheduling retry.",
                        LogSanitizer.Sanitize(item.JobId),
                        nextRetry,
                        failed.MaxRetries);

                    _info[item.JobId] = failed with { State = BackgroundJobState.Pending, RetryCount = nextRetry, Error = ex.Message };

                    int delayMs = (int)Math.Min(1000 * Math.Pow(2, nextRetry - 1), 30_000);
                    await Task.Delay(delayMs, stoppingToken);

                    if (!await _pendingJobs.WaitAsync(0, stoppingToken))
                    {
                        logger.LogError(
                            "Background job {JobId} could not be re-queued; pending capacity exhausted.",
                            LogSanitizer.Sanitize(item.JobId));

                        _info[item.JobId] = failed with
                        {
                            State = BackgroundJobState.Failed,
                            CompletedUtc = DateTimeOffset.UtcNow,
                            RetryCount = nextRetry,
                            Error = "Retry skipped: job queue at capacity."
                        };
                    }
                    else if (!_queue.Writer.TryWrite(item))
                    {
                        _pendingJobs.Release();

                        logger.LogError("Background job {JobId} could not be re-queued; writer rejected item.", LogSanitizer.Sanitize(item.JobId));

                        _info[item.JobId] = failed with
                        {
                            State = BackgroundJobState.Failed,
                            CompletedUtc = DateTimeOffset.UtcNow,
                            RetryCount = nextRetry,
                            Error = "Retry skipped: queue writer not accepting jobs."
                        };
                    }
                }
                else
                {
                    logger.LogError(
                        ex,
                        "Background job {JobId} failed after {Attempts} attempt(s); moving to DLQ.",
                        LogSanitizer.Sanitize(item.JobId),
                        nextRetry);

                    _info[item.JobId] = failed with
                    {
                        State = BackgroundJobState.Failed, CompletedUtc = DateTimeOffset.UtcNow, RetryCount = nextRetry, Error = ex.Message
                    };
                }
            }

            EvictOldTerminalJobs();
        }
    }

    private void EvictOldTerminalJobs()
    {
        List<BackgroundJobInfo> terminal = _info.Values
            .Where(j => j.State is BackgroundJobState.Succeeded or BackgroundJobState.Failed)
            .OrderBy(j => j.CompletedUtc)
            .ToList();

        if (terminal.Count <= InMemoryBackgroundJobQueueLimits.MaxRetainedTerminalJobs)
            return;

        foreach (BackgroundJobInfo old in terminal.Take(terminal.Count - InMemoryBackgroundJobQueueLimits.MaxRetainedTerminalJobs))
        {
            _info.TryRemove(old.JobId, out _);
            _files.TryRemove(old.JobId, out _);
        }
    }
}
