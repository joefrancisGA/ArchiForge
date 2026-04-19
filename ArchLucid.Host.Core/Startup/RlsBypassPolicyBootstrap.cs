using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.Connections;

namespace ArchLucid.Host.Core.Startup;

/// <summary>
/// Wires <see cref="SqlRowLevelSecurityBypassAmbient"/> break-glass policy and RLS bypass observability before SQL bootstrap.
/// </summary>
public static class RlsBypassPolicyBootstrap
{
    /// <summary>
    /// Idempotent: safe to call from API and Worker before <see cref="ArchLucidPersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed"/>.
    /// </summary>
    public static void Apply(IConfiguration configuration, IHostEnvironment environment, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);

        SqlRowLevelSecurityBypassAmbient.ConfigureBypassPolicy(
            breakGlassEnabled: () => RlsBreakGlass.IsEnabled(configuration),
            strictBypassRequired: () =>
                configuration.GetValue("SqlServer:RowLevelSecurity:ApplySessionContext", false));

        bool enabled = RlsBreakGlass.IsEnabled(configuration);
        bool prodLike = HostEnvironmentClassification.IsProductionOrStagingLike(environment, configuration);
        long gaugeValue = enabled && prodLike ? 1L : 0L;
        ArchLucidInstrumentation.SetRlsBypassProductionLikeEnabled(gaugeValue);
        ArchLucidInstrumentation.EnsureOutboxDepthObservableGaugesRegistered();

        if (!enabled || !logger.IsEnabled(LogLevel.Warning))
            return;

        logger.LogWarning(
            "SQL RLS break-glass is enabled (ARCHLUCID_ALLOW_RLS_BYPASS=true and ArchLucid:Persistence:AllowRlsBypass=true). "
            + "SqlRowLevelSecurityBypassAmbient.Enter may set af_rls_bypass=1 when session context is applied.");
    }
}
