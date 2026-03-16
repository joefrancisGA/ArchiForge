using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ArchiForge.Api.Jobs;

public sealed class InMemoryBackgroundJobQueue : BackgroundService, IBackgroundJobQueue
{
    private sealed record WorkItem(
        string JobId,
        string? FileNameHint,
        string? ContentTypeHint,
        Func<CancellationToken, Task<BackgroundJobFile>> Work);

    private readonly Channel<WorkItem> _queue = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    private readonly ConcurrentDictionary<string, BackgroundJobInfo> _info = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, BackgroundJobFile> _files = new(StringComparer.Ordinal);

    public string Enqueue(string? fileNameHint, string? contentTypeHint, Func<CancellationToken, Task<BackgroundJobFile>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        var id = Guid.NewGuid().ToString("n");
        var now = DateTimeOffset.UtcNow;

        _info[id] = new BackgroundJobInfo(
            JobId: id,
            State: BackgroundJobState.Pending,
            CreatedUtc: now,
            StartedUtc: null,
            CompletedUtc: null,
            Error: null,
            FileName: fileNameHint,
            ContentType: contentTypeHint);

        _queue.Writer.TryWrite(new WorkItem(id, fileNameHint, contentTypeHint, work));
        return id;
    }

    public BackgroundJobInfo? GetInfo(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return null;
        return _info.TryGetValue(jobId, out var info) ? info : null;
    }

    public BackgroundJobFile? GetFile(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return null;
        return _files.TryGetValue(jobId, out var file) ? file : null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_info.TryGetValue(item.JobId, out var current))
                continue;

            _info[item.JobId] = current with
            {
                State = BackgroundJobState.Running,
                StartedUtc = DateTimeOffset.UtcNow
            };

            try
            {
                var file = await item.Work(stoppingToken);
                _files[item.JobId] = file;

                var done = _info[item.JobId];
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
                var failed = _info[item.JobId];
                _info[item.JobId] = failed with
                {
                    State = BackgroundJobState.Failed,
                    CompletedUtc = DateTimeOffset.UtcNow,
                    Error = ex.Message
                };
            }
        }
    }
}

