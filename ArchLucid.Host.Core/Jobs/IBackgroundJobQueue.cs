using ArchiForge.Application.Jobs;

namespace ArchiForge.Host.Core.Jobs;

public interface IBackgroundJobQueue
{
    Task<string> EnqueueAsync(
        BackgroundJobWorkUnit workUnit,
        int maxRetries = 0,
        CancellationToken cancellationToken = default);

    Task<BackgroundJobInfo?> GetInfoAsync(string jobId, CancellationToken cancellationToken = default);

    Task<BackgroundJobFile?> GetFileAsync(string jobId, CancellationToken cancellationToken = default);
}
