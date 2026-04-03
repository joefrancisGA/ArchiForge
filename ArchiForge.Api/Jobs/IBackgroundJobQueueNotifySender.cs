namespace ArchiForge.Api.Jobs;

public interface IBackgroundJobQueueNotifySender
{
    Task SendJobIdAsync(string jobId, CancellationToken cancellationToken = default);
}
