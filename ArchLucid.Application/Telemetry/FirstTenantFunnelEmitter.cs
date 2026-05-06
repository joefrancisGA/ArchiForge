using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Telemetry;
/// <summary>
///     Improvement 12 emitter. Always increments the aggregated <c>archlucid_first_tenant_funnel_events_total</c>
///     counter. When <c>Telemetry:FirstTenantFunnel:PerTenantEmission</c> is on, additionally tags the
///     metric with <c>tenant_id</c> and persists a row via <see cref = "IFirstTenantFunnelEventStore"/>.
///     Reads the flag through <see cref = "IOptionsMonitor{TOptions}"/> so an owner flip takes effect at
///     the next emit without restart.
/// </summary>
public sealed class FirstTenantFunnelEmitter(IOptionsMonitor<FirstTenantFunnelOptions> optionsMonitor, IFirstTenantFunnelEventStore eventStore, TimeProvider timeProvider, ILogger<FirstTenantFunnelEmitter> logger) : IFirstTenantFunnelEmitter
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(optionsMonitor, eventStore, timeProvider, logger);
    private static byte __ValidatePrimaryConstructorArguments(Microsoft.Extensions.Options.IOptionsMonitor<ArchLucid.Core.Configuration.FirstTenantFunnelOptions> optionsMonitor, ArchLucid.Persistence.Telemetry.IFirstTenantFunnelEventStore eventStore, System.TimeProvider timeProvider, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.Telemetry.FirstTenantFunnelEmitter> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    private readonly IFirstTenantFunnelEventStore _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    private readonly ILogger<FirstTenantFunnelEmitter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOptionsMonitor<FirstTenantFunnelOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    /// <inheritdoc/>
    public async Task EmitAsync(string eventName, Guid tenantId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        if (!FirstTenantFunnelEventNames.IsValid(eventName))
            throw new ArgumentOutOfRangeException(nameof(eventName), eventName, $"eventName must be one of: {string.Join(", ", FirstTenantFunnelEventNames.All)}.");
        FirstTenantFunnelOptions options = _optionsMonitor.CurrentValue;
        bool perTenant = options.PerTenantEmission;
        string? tenantLabel = perTenant ? NormalizeTenantId(tenantId) : null;
        ArchLucidInstrumentation.RecordFirstTenantFunnelEvent(eventName, perTenant, tenantLabel);
        if (!perTenant)
            return;
        try
        {
            DateTime occurredUtc = _timeProvider.GetUtcNow().UtcDateTime;
            await _eventStore.AppendAsync(eventName, tenantId, occurredUtc, ct);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "First-tenant funnel per-tenant row append failed for event {FunnelEvent} tenant {TenantId}; aggregated counter still recorded.", eventName, tenantId);
        }
    }

    private static string NormalizeTenantId(Guid tenantId)
    {
        return tenantId == Guid.Empty ? string.Empty : tenantId.ToString("D");
    }
}