using ArchiForge.Api.Configuration;
using ArchiForge.Application.Jobs;

using ArchiForge.Data.Repositories;

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ArchiForge.Api.Jobs;

/// <summary>Worker-side loop: receives job ids from Azure Storage Queue, executes exports, stores results in blob.</summary>
public sealed class BackgroundJobQueueProcessorHostedService(
    ILogger<BackgroundJobQueueProcessorHostedService> logger,
    QueueClient queueClient,
    IBackgroundJobRepository repository,
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobsOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BackgroundJobsOptions snapshot = options.Value;
        TimeSpan visibility = TimeSpan.FromMinutes(Math.Clamp(snapshot.ProcessorVisibilityMinutes, 1, 120));
        int idleMs = Math.Clamp(snapshot.ProcessorIdlePollMilliseconds, 100, 60_000);

        await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(
                    maxMessages: 1,
                    visibilityTimeout: visibility,
                    cancellationToken: stoppingToken);

                if (messages.Length == 0)
                {
                    await Task.Delay(idleMs, stoppingToken);

                    continue;
                }

                QueueMessage message = messages[0];
                string? jobId = message.MessageText?.Trim();

                if (string.IsNullOrWhiteSpace(jobId))
                {
                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

                    continue;
                }

                await ProcessOneMessageAsync(jobId, message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background job queue processor loop failed; backing off.");
                await Task.Delay(idleMs, stoppingToken);
            }
        }
    }

    private async Task ProcessOneMessageAsync(string jobId, QueueMessage message, CancellationToken stoppingToken)
    {
        BackgroundJobsOptions snapshot = options.Value;

        BackgroundJobRow? row = await repository.GetAsync(jobId, stoppingToken);

        if (row is null)
        {
            logger.LogWarning("Queue message for unknown job id {JobId}; deleting stale message.", jobId);
            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

            return;
        }

        if (string.Equals(row.State, nameof(BackgroundJobState.Succeeded), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(row.State, nameof(BackgroundJobState.Failed), StringComparison.OrdinalIgnoreCase))
        {
            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

            return;
        }

        int claimed = await repository.TryMarkRunningAsync(jobId, stoppingToken);

        if (claimed == 0)
        {
            BackgroundJobRow? current = await repository.GetAsync(jobId, stoppingToken);

            if (current is not null &&
                string.Equals(current.State, nameof(BackgroundJobState.Running), StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Job {JobId} already running elsewhere; leaving message for retry visibility.", jobId);
            }

            return;
        }

        BackgroundJobWorkUnit? workUnit = BackgroundJobWorkUnitJson.Deserialize(row.WorkUnitJson);

        if (workUnit is null)
        {
            logger.LogError("Job {JobId} has invalid WorkUnitJson; failing permanently.", jobId);
            await repository.MarkFailedTerminalAsync(jobId, "Invalid job payload.", row.RetryCount + 1, stoppingToken);
            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

            return;
        }

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IBackgroundJobWorkUnitExecutor executor = scope.ServiceProvider.GetRequiredService<IBackgroundJobWorkUnitExecutor>();
            IBackgroundJobResultBlobAccessor blobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobResultBlobAccessor>();

            BackgroundJobFile file = await executor.ExecuteAsync(workUnit, stoppingToken);
            string blobName = await blobs.UploadAsync(jobId, file, stoppingToken);

            await repository.MarkSucceededAsync(jobId, blobName, file.FileName, file.ContentType, stoppingToken);
            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(jobId, row, ex, message, options.Value.MaxPendingJobs, stoppingToken);
        }
    }

    private async Task HandleFailureAsync(
        string jobId,
        BackgroundJobRow row,
        Exception ex,
        QueueMessage message,
        int maxPendingJobs,
        CancellationToken stoppingToken)
    {
        int nextRetry = row.RetryCount + 1;

        if (nextRetry <= row.MaxRetries)
        {
            logger.LogWarning(
                ex,
                "Background job {JobId} failed (attempt {Attempt}/{Max}); scheduling retry.",
                jobId,
                nextRetry,
                row.MaxRetries);

            await repository.MarkPendingRetryAsync(jobId, nextRetry, ex.Message, stoppingToken);

            int delayMs = (int)Math.Min(1000 * Math.Pow(2, nextRetry - 1), 30_000);
            await Task.Delay(delayMs, stoppingToken);

            int pending = await repository.CountNonTerminalAsync(stoppingToken);

            if (pending >= maxPendingJobs)
            {
                logger.LogError(
                    "Background job {JobId} could not be re-queued; non-terminal capacity exhausted.",
                    jobId);

                await repository.MarkFailedTerminalAsync(
                    jobId,
                    "Retry skipped: job queue at capacity.",
                    nextRetry,
                    stoppingToken);

                await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

                return;
            }

            await queueClient.SendMessageAsync(jobId, stoppingToken);
            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

            return;
        }

        logger.LogError(
            ex,
            "Background job {JobId} failed after {Attempts} attempt(s).",
            jobId,
            nextRetry);

        await repository.MarkFailedTerminalAsync(jobId, ex.Message, nextRetry, stoppingToken);
        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
    }
}
