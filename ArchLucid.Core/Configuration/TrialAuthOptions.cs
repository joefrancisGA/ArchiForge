namespace ArchLucid.Core.Configuration;

/// <summary>Configuration for self-service trial authentication surfaces (Entra External ID / local identity).</summary>
public sealed class TrialAuthOptions
{
    public const string SectionPath = "Auth:Trial";

    /// <summary>Enabled trial auth modes (case-insensitive): <see cref="TrialAuthModeConstants" />.</summary>
    public List<string> Modes
    {
        get;
        set;
    } = [];

    /// <summary>Entra External ID directory (tenant) id — required in Production when <c>MsaExternalId</c> mode is enabled.</summary>
    public string? ExternalIdTenantId
    {
        get;
        set;
    }

    public TrialLocalIdentityOptions LocalIdentity
    {
        get;
        set;
    } = new();
}

/// <summary>Local email/password trial auth (PBKDF2 via ASP.NET Identity password hasher; Dapper-backed users table).</summary>
public sealed class TrialLocalIdentityOptions
{
    public const string SectionPath = TrialAuthOptions.SectionPath + ":LocalIdentity";

    /// <summary>
    ///     RSA private key PEM path used to mint short-lived API JWTs after password validation (must pair with
    ///     ArchLucidAuth public PEM validation).
    /// </summary>
    public string JwtPrivateKeyPemPath
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     Issuer and audience copied into minted JWTs; must match ArchLucidAuth JwtLocalIssuer / JwtLocalAudience when
    ///     using local PEM validation.
    /// </summary>
    public string JwtIssuer
    {
        get;
        set;
    } = string.Empty;

    public string JwtAudience
    {
        get;
        set;
    } = string.Empty;

    public int AccessTokenLifetimeMinutes
    {
        get;
        set;
    } = 60;

    public int MaxFailedAccessAttemptsBeforeLockout
    {
        get;
        set;
    } = 5;

    public int LockoutMinutes
    {
        get;
        set;
    } = 15;

    /// <summary>When true, password changes/registrations call the HIBP k-anonymity range API (5-prefix SHA-1 range query).</summary>
    public bool PwnedPasswordRangeCheckEnabled
    {
        get;
        set;
    }

    public int MinimumPasswordLength
    {
        get;
        set;
    } = 8;

    public int MaximumPasswordLength
    {
        get;
        set;
    } = 128;
}
