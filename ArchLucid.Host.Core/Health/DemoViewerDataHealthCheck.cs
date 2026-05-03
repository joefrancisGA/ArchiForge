using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Health;

/// <summary>
///     When anonymous demo viewer is enabled, verifies at least one committed demo-seed run exists so public URLs are
///     not empty.
/// </summary>
public sealed class DemoViewerDataHealthCheck(
    IOptions<DemoOptions> demoOptions,
    IDemoSeedRunResolver demoSeedRunResolver) : IHealthCheck
{
    private readonly IOptions<DemoOptions> _demoOptions =
        demoOptions ?? throw new ArgumentNullException(nameof(demoOptions));

    private readonly IDemoSeedRunResolver _demoSeedRunResolver =
        demoSeedRunResolver ?? throw new ArgumentNullException(nameof(demoSeedRunResolver));

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_demoOptions.Value.AnonymousViewer.Enabled)
        {
            return HealthCheckResult.Healthy("Demo anonymous viewer is disabled.");
        }

        RunRecord? run = await _demoSeedRunResolver.ResolveLatestCommittedDemoRunAsync(cancellationToken);

        return run is null
            ? HealthCheckResult.Degraded("Anonymous demo viewer enabled but no committed Contoso demo run exists.")
            : HealthCheckResult.Healthy("Anonymous demo viewer has committed demo data.");
    }
}
