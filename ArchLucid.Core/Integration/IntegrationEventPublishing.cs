using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Integration;

/// <summary>Best-effort publishing helpers for <see cref="IIntegrationEventPublisher"/>.</summary>
/// <remarks>Failures are logged and swallowed so domain commits are not rolled back.</remarks>
public static class IntegrationEventPublishing
{
    /// <summary>Publishes <paramref name="payload"/> as UTF-8 JSON when <paramref name="publisher"/> is non-null.</summary>
    public static async Task TryPublishAsync(
        IIntegrationEventPublisher publisher,
        ILogger logger,
        string eventType,
        object payload,
        string? messageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(payload, IntegrationEventJson.Options);

            await publisher.PublishAsync(eventType, utf8, messageId, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested
                                   && ex is not OutOfMemoryException
                                   && ex is not StackOverflowException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Integration event publish failed for {EventType}", eventType);
            }
        }
    }
}
