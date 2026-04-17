using System.Security.Claims;

namespace ArchLucid.Api.Auth.Models;

public class ArchLucidAuthOptions
{
    public const string SectionName = "ArchLucidAuth";

    /// <summary>DevelopmentBypass | JwtBearer | ApiKey</summary>
    public string Mode { get; set; } = "ApiKey";
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// When true and <see cref="JwtSigningPublicKeyPemPath"/> is empty, JWT issuer must be an Azure AD v2.0 issuer
    /// (<c>login.microsoftonline.com/{tid}/v2.0</c>) and optional <see cref="AllowedEntraTenantIds"/> restricts <c>tid</c>.
    /// </summary>
    public bool MultiTenantEntra { get; set; }

    /// <summary>Comma-separated Entra directory tenant ids (<c>tid</c>). When non-empty, tokens must include a matching <c>tid</c> claim.</summary>
    public string AllowedEntraTenantIds { get; set; } = string.Empty;

    /// <summary>
    /// Claim type used as the user name after JWT validation. Entra ID tokens often use
    /// <c>preferred_username</c> or <c>name</c>; default matches classic <see cref="ClaimTypes.Name"/>.
    /// </summary>
    public string NameClaimType { get; set; } = ClaimTypes.Name;
    public string DevUserId { get; set; } = "dev-user";
    public string DevUserName { get; set; } = "Developer";

    /// <summary>Admin | Operator | Reader</summary>
    public string DevRole { get; set; } = "Admin";

    /// <summary>
    /// Path to a PEM file containing an RSA <strong>public</strong> key for JWT signature validation.
    /// When set, OIDC metadata from <see cref="Authority"/> is not used (CI / local signing-key E2E only).
    /// </summary>
    public string JwtSigningPublicKeyPemPath { get; set; } = string.Empty;

    /// <summary><c>iss</c> claim value when using <see cref="JwtSigningPublicKeyPemPath"/>.</summary>
    public string JwtLocalIssuer { get; set; } = string.Empty;

    /// <summary><c>aud</c> claim value when using <see cref="JwtSigningPublicKeyPemPath"/>.</summary>
    public string JwtLocalAudience { get; set; } = string.Empty;

    /// <summary>
    /// When true in Production, <see cref="Mode"/> must be <c>JwtBearer</c> (Entra / OIDC). Use for regulated SaaS
    /// tenants that disallow API-key authentication in production; default is false so ApiKey remains valid for pilots.
    /// </summary>
    public bool RequireJwtBearerInProduction { get; set; }
}
