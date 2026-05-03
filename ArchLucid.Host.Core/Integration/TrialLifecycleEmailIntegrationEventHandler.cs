using System.Text.Json;

using ArchLucid.Application.Notifications.Email;
using ArchLucid.Core.Integration;

namespace ArchLucid.Host.Core.Integration;

public sealed class TrialLifecycleEmailIntegrationEventHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<TrialLifecycleEmailIntegrationEventHandler> logger) : IIntegrationEventHandler
{
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly ILogger<TrialLifecycleEmailIntegrationEventHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string EventType => IntegrationEventTypes.TrialLifecycleEmailV1;

    /// <inheritdoc />
    public async Task HandleAsync(ReadOnlyMemory<byte> utf8JsonPayload, CancellationToken cancellationToken)
    {
        TrialLifecycleEmailIntegrationEnvelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<TrialLifecycleEmailIntegrationEnvelope>(
                utf8JsonPayload.Span,
                IntegrationEventJson.Options);
        }
        catch (JsonException ex)
        {
            throw new FormatException("Trial lifecycle email payload was not valid JSON.", ex);
        }

        if (envelope is null)
            throw new FormatException("Trial lifecycle email payload deserialized to null.");

        using IServiceScope scope = _scopeFactory.CreateScope();
        ITrialLifecycleEmailDispatcher dispatcher =
            scope.ServiceProvider.GetRequiredService<ITrialLifecycleEmailDispatcher>();

        if (_logger.IsEnabled(LogLevel.Debug))

            _logger.LogDebug(
                "Dispatching trial lifecycle email trigger {Trigger} for tenant {TenantId}.",
                envelope.Trigger,
                envelope.TenantId);

        await dispatcher.DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
    }
}
