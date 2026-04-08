using System.Diagnostics;
using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Resilience;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Core.Services;

/// <summary>
/// Produces a hot-path-safe audit callback for <see cref="CircuitBreakerGate"/> (fire-and-forget; failures are swallowed).
/// </summary>
public sealed class CircuitBreakerAuditBridge(
    IServiceScopeFactory scopeFactory,
    IScopeContextProvider scopeProvider,
    ILogger<CircuitBreakerAuditBridge> logger)
{
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly ILogger<CircuitBreakerAuditBridge> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Builds a callback suitable for <see cref="CircuitBreakerGate"/>; never throws to the breaker.</summary>
    public Action<CircuitBreakerAuditEntry> CreateCallback()
    {
        return entry =>
        {
            try
            {
                ScopeContext scope = _scopeProvider.GetCurrentScope();
                string? correlationCapture = Activity.Current?.Id;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using IServiceScope serviceScope = _scopeFactory.CreateScope();
                        IAuditService audit = serviceScope.ServiceProvider.GetRequiredService<IAuditService>();

                        string eventType = entry.TransitionType switch
                        {
                            "StateTransition" => AuditEventTypes.CircuitBreakerStateTransition,
                            "Rejection" => AuditEventTypes.CircuitBreakerRejection,
                            "ProbeOutcome" => AuditEventTypes.CircuitBreakerProbeOutcome,
                            _ => "CircuitBreakerUnknown",
                        };

                        AuditEvent auditEvent = new()
                        {
                            EventType = eventType,
                            ActorUserId = "system",
                            ActorUserName = "CircuitBreakerGate",
                            TenantId = scope.TenantId,
                            WorkspaceId = scope.WorkspaceId,
                            ProjectId = scope.ProjectId,
                            OccurredUtc = entry.OccurredUtc.UtcDateTime,
                            DataJson = JsonSerializer.Serialize(
                                new
                                {
                                    gate = entry.GateName,
                                    fromState = entry.FromState,
                                    toState = entry.ToState,
                                    probeOutcome = entry.ProbeOutcome,
                                }),
                            CorrelationId = correlationCapture,
                        };

                        await audit.LogAsync(auditEvent, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Circuit breaker audit append failed for gate {GateName}",
                            entry.GateName);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Circuit breaker audit scheduling failed");
            }
        };
    }
}
