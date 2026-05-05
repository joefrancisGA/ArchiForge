namespace ArchLucid.Host.Core.Configuration;

/// <summary>Product defaults for rate limiting when configuration keys are absent.</summary>
public static class RateLimitingDefaults
{
    /// <summary>Default requests per <c>RateLimiting:FixedWindow:WindowMinutes</c> for the <c>fixed</c> policy.</summary>
    public const int FixedWindowPermitLimit = 100;

    /// <summary>
    ///     Default requests per window for <c>governancePolicyPackDryRun</c> (
    ///     <c>POST /v1/governance/policy-packs/{{id}}/dry-run</c>); partitioned per authenticated user (
    ///     <c>NameIdentifier</c> claim).
    /// </summary>
    public const int GovernancePolicyPackDryRunPermitLimit = 12;
}
