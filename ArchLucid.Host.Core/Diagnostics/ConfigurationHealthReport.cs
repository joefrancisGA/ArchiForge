namespace ArchLucid.Host.Core.Diagnostics;

/// <summary>
///     Serializable connectivity snapshot for operator onboarding (SQL, OIDC authority, optional Key Vault).
/// </summary>
public sealed class ConfigurationHealthReport
{
    public required IReadOnlyList<ConfigurationHealthCheckResult> Checks
    {
        get;
        init;
    }
}

/// <summary>One logical probe outcome (never contains secret values).</summary>
public sealed class ConfigurationHealthCheckResult
{
    public required string Name
    {
        get;
        init;
    }

    public required string Status
    {
        get;
        init;
    }

    public string? Detail
    {
        get;
        init;
    }
}
