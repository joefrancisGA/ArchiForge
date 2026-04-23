namespace ArchLucid.Api.Attributes;

/// <summary>
///     Stable, refactor-safe identifiers for routes that are hidden behind a per-deployment feature toggle.
///     Each value maps inside <see cref="ArchLucid.Api.Filters.FeatureGateFilter" /> to a single configuration flag —
///     the enum exists (instead of bare strings) so the surface cannot drift if the underlying option moves.
/// </summary>
public enum FeatureGateKey
{
    /// <summary>
    ///     The route requires <c>Demo:Enabled=true</c> (<see cref="ArchLucid.Core.Configuration.DemoOptions" />).
    ///     Returns <c>404 Not Found</c> on production-like deployments so the demo surface cannot be hit accidentally.
    /// </summary>
    DemoEnabled = 1
}
