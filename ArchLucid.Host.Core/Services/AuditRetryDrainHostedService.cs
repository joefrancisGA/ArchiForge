using ArchLucid.Core.Audit;

namespace ArchLucid.Host.Core.Services;

/// <summary>
/// Drains <see cref="IAuditRetryQueue"/> into <see cref="IAuditService"/> on a background thread.
/// </summary>
public sealed class AuditRetryDrainHostedService(
    IAuditRetryQueue auditRetryQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditRetryDrainHostedService> logger) : BackgroundService
{
    private readonly IAuditRetryQueue _auditRetryQueue =
        auditRetryQueue ?? throw new ArgumentNullException(nameof(auditRetryQueue));

    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly ILogger<AuditRetryDrainHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            AuditEvent auditEvent;

            try
            {
                auditEvent = await _auditRetryQueue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IAuditService audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
                await audit.LogAsync(auditEvent, stoppingToken);
                _auditRetryQueue.NotifyPersistedSuccess();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (_auditRetryQueue.TryReturnToQueueAfterFailedDrain(auditEvent))
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(ex, "Audit retry drain failed; event re-queued for a later attempt.");
                    }
                }
                else if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        ex,
                        "Audit retry drain failed and re-queue dropped (queue full); audit event may be lost.");
                }
            }
        }
    }
}
