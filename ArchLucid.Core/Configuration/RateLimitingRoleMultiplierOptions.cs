namespace ArchLucid.Core.Configuration;

/// <summary>
///     Multipliers applied to base <c>RateLimiting:FixedWindow:PermitLimit</c> and
///     <c>RateLimiting:Expensive:PermitLimit</c>
///     after authentication is resolved (partition remains per role + client IP).
/// </summary>
public sealed class RateLimitingRoleMultiplierOptions
{
    public const string SectionPath = "RateLimiting:RoleMultipliers";

    /// <summary>Multiplier when the principal has the Admin role (default 3).</summary>
    public double Admin
    {
        get;
        set;
    } = 3.0;

    /// <summary>Multiplier when the principal has the Operator role (default 1.5).</summary>
    public double Operator
    {
        get;
        set;
    } = 1.5;

    /// <summary>Multiplier for Reader, Auditor, and other authenticated roles (default 1).</summary>
    public double Reader
    {
        get;
        set;
    } = 1.0;

    /// <summary>Multiplier when the request is not authenticated (default 0.5).</summary>
    public double Anonymous
    {
        get;
        set;
    } = 0.5;
}
