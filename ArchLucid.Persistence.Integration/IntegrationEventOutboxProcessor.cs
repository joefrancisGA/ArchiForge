using ArchiForge.Core.Integration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiForge.Persistence.Integration;

/// <inheritdoc cref="IIntegrationEventOutboxProcessor" />
public sealed class IntegrationEventOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<IntegrationEventsOptions> integrationEventsOptions,
    ILogger<IntegrationEventOutboxProcessor> logger) : IIntegrationEventOutboxProcessor
{
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly IOptions<IntegrationEventsOptions> _integrationEventsOptions =
        integrationEventsOptions ?? throw new ArgumentNullException(nameof(integrationEventsOptions));

    private readonly ILogger<IntegrationEventOutboxProcessor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task ProcessPendingBatchAsync(CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IIntegrationEventOutboxRepository outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutboxRepository>();
        IIntegrationEventPublisher publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        IntegrationEventsOptions opts = _integrationEventsOptions.Value;
        int maxAttempts = Math.Clamp(opts.OutboxMaxPublishAttempts, 1, 100);
        int maxBackoffSeconds = Math.Clamp(opts.OutboxMaxBackoffSeconds, 1, 86_400);

        IReadOnlyList<IntegrationEventOutboxEntry> batch = await outbox.DequeuePendingAsync(25, ct);

        foreach (IntegrationEventOutboxEntry entry in batch)
        {
            try
            {
                await publisher.PublishAsync(
                    entry.EventType,
                    entry.PayloadUtf8,
                    entry.MessageId,
                    ct);

                await outbox.MarkProcessedAsync(entry.OutboxId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                int newRetryCount = entry.RetryCount + 1;
                string err = ex.Message;

                if (err.Length > 2048)
                {
                    err = err[..2048];
                }

                if (newRetryCount >= maxAttempts)
                {
                    await outbox.RecordPublishFailureAsync(
                        entry.OutboxId,
                        newRetryCount,
                        nextRetryUtc: null,
                        deadLetteredUtc: DateTime.UtcNow,
                        lastErrorMessage: err,
                        ct);

                    _logger.LogError(
                        ex,
                        "Integration event outbox dead-lettered after {RetryCount} failures (outbox {OutboxId}, event {EventType}).",
                        newRetryCount,
                        entry.OutboxId,
                        entry.EventType);
                }
                else
                {
                    TimeSpan delay = IntegrationEventOutboxRetryCalculator.DelayUntilNextAttempt(newRetryCount, maxBackoffSeconds);
                    DateTime nextUtc = DateTime.UtcNow.Add(delay);

                    await outbox.RecordPublishFailureAsync(
                        entry.OutboxId,
                        newRetryCount,
                        nextRetryUtc: nextUtc,
                        deadLetteredUtc: null,
                        lastErrorMessage: err,
                        ct);

                    _logger.LogWarning(
                        ex,
                        "Integration event outbox publish failed (attempt {RetryCount}/{Max}); next retry after {NextRetryUtc} (outbox {OutboxId}, event {EventType}).",
                        newRetryCount,
                        maxAttempts,
                        nextUtc,
                        entry.OutboxId,
                        entry.EventType);
                }
            }
        }
    }
}
