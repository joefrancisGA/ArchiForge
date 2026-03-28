using Microsoft.Extensions.Logging;

namespace ArchiForge.Application;

/// <inheritdoc cref="IBaselineMutationAuditService"/>
public sealed class BaselineMutationAuditService(ILogger<BaselineMutationAuditService> logger)
    : IBaselineMutationAuditService
{
    private const int MaxDetailsLength = 500;

    /// <inheritdoc />
    public Task RecordAsync(
        string eventType,
        string actor,
        string entityId,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        string safeDetails = TruncateDetails(details);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "BaselineMutation {EventType} Actor={Actor} EntityId={EntityId} Details={Details}",
                eventType,
                actor,
                entityId,
                safeDetails);
        }

        return Task.CompletedTask;
    }

    private static string TruncateDetails(string? details)
    {
        if (string.IsNullOrEmpty(details))
            return string.Empty;

        string trimmed = details.Trim();

        if (trimmed.Length <= MaxDetailsLength)
            return trimmed;

        return trimmed[..MaxDetailsLength] + "…";
    }
}
