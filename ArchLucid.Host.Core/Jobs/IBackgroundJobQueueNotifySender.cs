namespace ArchiForge.Host.Core.Jobs;

public interface IBackgroundJobQueueNotifySender
{
    Task SendJobIdAsync(string jobId, CancellationToken cancellationToken = default);
}
