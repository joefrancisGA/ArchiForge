using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Common;

/// <inheritdoc cref="IBaselineMutationAuditService" />
public sealed class BaselineMutationAuditService(
    ILogger<BaselineMutationAuditService> logger,
    IAuditService auditService,
    IScopeContextProvider scopeContextProvider)
    : IBaselineMutationAuditService
{
    private const int MaxDetailsLength = 500;

    /// <inheritdoc />
    public async Task RecordAsync(
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
                LogSanitizer.Sanitize(eventType),
                LogSanitizer.Sanitize(actor),
                LogSanitizer.Sanitize(entityId),
                LogSanitizer
                    .Sanitize(safeDetails)); // codeql[cs/log-forging]: string placeholders sanitized via LogSanitizer; details length-capped in TruncateDetails.
        }

        if (!IsArchitectureBaselineMutationEvent(eventType))
            return;

        try
        {
            await BaselineMutationAuditArchitectureDurableWriter.TryWriteArchitectureDurableEchoAsync(
                eventType,
                actor,
                entityId,
                safeDetails,
                auditService,
                scopeContextProvider,
                logger,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    ex,
                    "Baseline durable echo failed for {EventType} EntityId={EntityId}",
                    LogSanitizer.Sanitize(eventType),
                    LogSanitizer.Sanitize(entityId));
            }
        }
    }

    private static bool IsArchitectureBaselineMutationEvent(string eventType)
    {
        return string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunFailed, StringComparison.Ordinal)
               || string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunCreated, StringComparison.Ordinal)
               || string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunStarted, StringComparison.Ordinal)
               || string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunExecuteSucceeded,
                   StringComparison.Ordinal)
               || string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunCompleted,
                   StringComparison.Ordinal);
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
