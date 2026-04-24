using ArchLucid.Application.Telemetry;
using ArchLucid.Core.Configuration;

namespace ArchLucid.Host.Composition.Startup;

/// <summary>
///     Improvement 12 — DI registrations for the first-tenant onboarding telemetry funnel.
///
///     The <c>IFirstTenantFunnelEventStore</c> implementation is selected by the storage provider
///     registrar (<c>SqlStorageProviderRegistrar</c> wires <c>SqlFirstTenantFunnelEventStore</c>;
///     <c>InMemoryStorageProviderRegistrar</c> wires <c>NoopFirstTenantFunnelEventStore</c>).
///     The emitter itself is the same in every host, but it only writes per-tenant rows when the
///     owner-only flag <c>Telemetry:FirstTenantFunnel:PerTenantEmission</c> is on (default
///     <c>false</c>) — see <c>docs/security/PRIVACY_NOTE.md</c> §3.A and pending question 40.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    ///     Binds <see cref="FirstTenantFunnelOptions" /> and registers
    ///     <see cref="IFirstTenantFunnelEmitter" /> as a scoped service so it can resolve the
    ///     scope-bound event store and current options snapshot.
    /// </summary>
    public static IServiceCollection AddFirstTenantFunnelTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.Configure<FirstTenantFunnelOptions>(
            configuration.GetSection(FirstTenantFunnelOptions.SectionName));

        services.AddScoped<IFirstTenantFunnelEmitter, FirstTenantFunnelEmitter>();

        return services;
    }
}
