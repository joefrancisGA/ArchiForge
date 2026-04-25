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
}
