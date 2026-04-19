using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Archival;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>
/// One archival pass with failure logging and audit emission — extracted for unit testing and health state.
/// </summary>
public static class DataArchivalHostIteration
{
    /// <summary>
    /// When <paramref name="options"/>.<see cref="DataArchivalOptions.Enabled"/> is true, runs the coordinator once;
    /// on failure logs, records health state, and best-effort persists <see cref="AuditEventTypes.DataArchivalHostLoopFailed"/>.
    /// </summary>
    /// <returns><see langword="true"/> when archival was disabled or the coordinator completed; <see langword="false"/> when enabled and the coordinator failed.</returns>
    public static async Task<bool> RunOnceAsync(
        IServiceScopeFactory scopeFactory,
        DataArchivalOptions options,
        ILogger logger,
        DataArchivalHostHealthState? healthState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (!options.Enabled)
        {
            return true;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        IDataArchivalCoordinator coordinator =
            scope.ServiceProvider.GetRequiredService<IDataArchivalCoordinator>();

        try
        {
            await coordinator.RunOnceAsync(options, cancellationToken);
            healthState?.MarkLastIterationSucceeded();

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data archival host loop error.");
            healthState?.MarkLastIterationFailed(ex);

            try
            {
                using IServiceScope auditScope = scopeFactory.CreateScope();
                IAuditService audit = auditScope.ServiceProvider.GetRequiredService<IAuditService>();

                await audit.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.DataArchivalHostLoopFailed,
                        DataJson = JsonSerializer.Serialize(
                            new
                            {
                                ex.Message,
                                ExceptionType = ex.GetType().FullName
                            })
                    },
                    CancellationToken.None);
            }
            catch (Exception auditEx)
            {
                logger.LogWarning(auditEx, "Failed to persist audit event for data archival host loop failure.");
            }

            return false;
        }
    }
}
