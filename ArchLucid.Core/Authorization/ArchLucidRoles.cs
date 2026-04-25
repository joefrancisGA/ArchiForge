namespace ArchLucid.Core.Authorization;

/// <summary>
///     Role names carried on JWT <c>roles</c> / <see cref="System.Security.Claims.ClaimTypes.Role" /> claims and
///     DevelopmentBypass.
/// </summary>
public static class ArchLucidRoles
{
    /// <summary>
    ///     Read-only access (runs, manifests, governance reads, audit list/search). Claim value <c>Reader</c> matches
    ///     typical Entra app-role strings.
    /// </summary>
    public const string Reader = "Reader";

    /// <summary>Documentation alias for <see cref="Reader" /> (same claim value).</summary>
    public const string ReadOnly = Reader;

    public const string Operator = "Operator";
    public const string Admin = "Admin";

    /// <summary>Read scope plus audit export and compliance-oriented audit access.</summary>
    public const string Auditor = "Auditor";
}
