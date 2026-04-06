namespace ArchiForge.Host.Core.Hosting;

/// <summary>
/// Subscribes to <see cref="IHostApplicationLifetime.ApplicationStopping"/> so operators see a clear log line
/// while <see cref="HostOptions.ShutdownTimeout"/> allows in-flight requests and <see cref="BackgroundService"/> work to drain.
/// </summary>
public sealed class GracefulShutdownNotificationHostedService(
    IHostApplicationLifetime lifetime,
    ILogger<GracefulShutdownNotificationHostedService> logger) : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime =
        lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    private readonly ILogger<GracefulShutdownNotificationHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private IDisposable? _registration;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _registration = _lifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation(
                "Application stopping; waiting up to host shutdown timeout for background work and requests to complete.");
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _registration?.Dispose();
        _registration = null;

        return Task.CompletedTask;
    }
}
