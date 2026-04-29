namespace ArchLucid.Core.Identity;

public interface ITrialIdentityUserRepository
{
    Task<TrialIdentityUserRecord?> GetByNormalizedEmailAsync(string normalizedEmail,
        CancellationToken cancellationToken);

    Task<Guid> CreatePendingUserAsync(
        string normalizedEmail,
        string email,
        string passwordHash,
        string securityStamp,
        string concurrencyStamp,
        string emailConfirmationTokenHash,
        DateTimeOffset emailConfirmationExpiresUtc,
        CancellationToken cancellationToken);

    Task<bool> TryConfirmEmailAsync(
        string normalizedEmail,
        string emailConfirmationTokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task RecordAccessFailedAsync(string normalizedEmail, int newCount, DateTimeOffset? lockoutEnd,
        CancellationToken cancellationToken);

    Task ResetAccessFailedAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>
    ///     Sets <c>LinkedEntraOid</c> / <c>LinkedUtc</c> when unset or idempotent same OID. No-op when the email is missing
    ///     or already linked to another OID.
    /// </summary>
    /// <returns><c>true</c> when the row was updated or already matched <paramref name="entraOid" />.</returns>
    Task<bool> TryLinkLocalIdentityToEntraAsync(string normalizedEmail, string entraOid, CancellationToken cancellationToken);
}
