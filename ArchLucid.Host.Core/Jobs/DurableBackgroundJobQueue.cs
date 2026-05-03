using ArchLucid.Host.Core.Configuration;
using ArchLucid.Application.Jobs;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Jobs;

public sealed class DurableBackgroundJobQueue(
    IBackgroundJobRepository repository,
    IBackgroundJobQueueNotifySender notifySender,
    IBackgroundJobResultBlobAccessor resultBlobs,
    IOptions<BackgroundJobsOptions> options) : IBackgroundJobQueue
{
    public async Task<string> EnqueueAsync(
        BackgroundJobWorkUnit workUnit,
        int maxRetries = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workUnit);

        BackgroundJobsOptions snapshot = options.Value;
        int safeMaxRetries = Math.Clamp(maxRetries, 0, 10);

        if (await repository.CountNonTerminalAsync(cancellationToken) >= snapshot.MaxPendingJobs)
            throw new InvalidOperationException(
                $"The background job queue is at capacity ({snapshot.MaxPendingJobs} non-terminal jobs). Try again later.");

        string jobId = Guid.NewGuid().ToString("N");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        string workJson = BackgroundJobWorkUnitJson.Serialize(workUnit);

        BackgroundJobRow row = new()
        {
            JobId = jobId,
            WorkUnitJson = workJson,
            State = nameof(BackgroundJobState.Pending),
            CreatedUtc = now,
            StartedUtc = null,
            CompletedUtc = null,
            Error = null,
            FileName = null,
            ContentType = null,
            RetryCount = 0,
            MaxRetries = safeMaxRetries,
            ResultBlobName = null
        };

        await repository.InsertAsync(row, cancellationToken);
        await notifySender.SendJobIdAsync(jobId, cancellationToken);

        return jobId;
    }

    public async Task<BackgroundJobInfo?> GetInfoAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return null;

        BackgroundJobRow? row = await repository.GetAsync(jobId, cancellationToken);

        return BackgroundJobPersistenceMapper.ToInfo(row);
    }

    public async Task<BackgroundJobFile?> GetFileAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return null;

        BackgroundJobRow? row = await repository.GetAsync(jobId, cancellationToken);

        if (row is null || string.IsNullOrWhiteSpace(row.ResultBlobName) || string.IsNullOrWhiteSpace(row.FileName) ||
            string.IsNullOrWhiteSpace(row.ContentType))
            return null;

        return await resultBlobs.DownloadAsync(row.ResultBlobName, row.FileName, row.ContentType, cancellationToken);
    }
}
