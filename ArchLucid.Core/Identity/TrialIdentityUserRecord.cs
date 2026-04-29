namespace ArchLucid.Core.Identity;

/// <summary>Row shape for <c>dbo.IdentityUsers</c> (trial local email/password).</summary>
public sealed class TrialIdentityUserRecord
{
    public Guid Id
    {
        get;
        init;
    }

    public string NormalizedEmail
    {
        get;
        init;
    } = string.Empty;

    public string Email
    {
        get;
        init;
    } = string.Empty;

    public string PasswordHash
    {
        get;
        init;
    } = string.Empty;

    public string SecurityStamp
    {
        get;
        init;
    } = string.Empty;

    public string ConcurrencyStamp
    {
        get;
        init;
    } = string.Empty;

    public bool EmailConfirmed
    {
        get;
        init;
    }

    public DateTimeOffset? EmailVerifiedUtc
    {
        get;
        init;
    }

    public DateTimeOffset? LockoutEnd
    {
        get;
        init;
    }

    public bool LockoutEnabled
    {
        get;
        init;
    }

    public int AccessFailedCount
    {
        get;
        init;
    }

    public string? EmailConfirmationTokenHash
    {
        get;
        init;
    }

    public DateTimeOffset? EmailConfirmationExpiresUtc
    {
        get;
        init;
    }

    /// <summary>Entra user object id (<c>oid</c>) after trial-to-paid handoff; null until linked.</summary>
    public string? LinkedEntraOid
    {
        get;
        init;
    }

    /// <summary>When <see cref="LinkedEntraOid" /> was set.</summary>
    public DateTimeOffset? LinkedUtc
    {
        get;
        init;
    }
}
