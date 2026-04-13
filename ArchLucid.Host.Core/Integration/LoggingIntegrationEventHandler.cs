using System.Text;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;

namespace ArchLucid.Host.Core.Integration;

/// <summary>Catch-all handler that records integration events at Information level (payload size + safe preview).</summary>
public sealed class LoggingIntegrationEventHandler(ILogger<LoggingIntegrationEventHandler> logger)
    : IIntegrationEventHandler
{
    private readonly ILogger<LoggingIntegrationEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string EventType => IntegrationEventTypes.WildcardEventType;

    /// <inheritdoc />
    public Task HandleAsync(ReadOnlyMemory<byte> utf8JsonPayload, CancellationToken ct)
    {
        int len = utf8JsonPayload.Length;
        string preview = BuildPreview(utf8JsonPayload);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Integration event received: payloadBytes={PayloadBytes}, preview={Preview}",
                len,
                LogSanitizer.Sanitize(preview));
        }

        return Task.CompletedTask;
    }

    private static string BuildPreview(ReadOnlyMemory<byte> utf8JsonPayload)
    {
        if (utf8JsonPayload.IsEmpty)
        {
            return string.Empty;
        }

        int take = Math.Min(utf8JsonPayload.Length, 256);

        try
        {
            return Encoding.UTF8.GetString(utf8JsonPayload.Span[..take]);
        }
        catch
        {
            return "<non-utf8>";
        }
    }
}
