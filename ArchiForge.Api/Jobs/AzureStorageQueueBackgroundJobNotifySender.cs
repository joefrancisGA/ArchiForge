using Azure.Storage.Queues;

namespace ArchiForge.Api.Jobs;

public sealed class AzureStorageQueueBackgroundJobNotifySender(QueueClient queueClient) : IBackgroundJobQueueNotifySender
{
    public async Task SendJobIdAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job id is required.", nameof(jobId));

        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await queueClient.SendMessageAsync(jobId, cancellationToken);
    }
}
