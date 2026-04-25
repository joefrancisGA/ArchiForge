using ArchLucid.Core.Identity;

namespace ArchLucid.Persistence.Identity;

/// <summary>
///     In-memory storage has no SQL <c>dbo.IdentityUsers</c>; local trial identity APIs are not supported on this
///     provider.
/// </summary>
public sealed class InMemoryNoTrialIdentityUserRepository : ITrialIdentityUserRepository
{
    /// <inheritdoc />
    public Task<TrialIdentityUserRecord?> GetByNormalizedEmailAsync(string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<TrialIdentityUserRecord?>(null);
    }

    /// <inheritdoc />
    public Task<Guid> CreatePendingUserAsync(
        string normalizedEmail,
        string email,
        string passwordHash,
        string securityStamp,
        string concurrencyStamp,
        string emailConfirmationTokenHash,
        DateTimeOffset emailConfirmationExpiresUtc,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Trial local identity requires ArchLucid:StorageProvider=Sql and migration 077 (dbo.IdentityUsers).");
    }

    /// <inheritdoc />
    public Task<bool> TryConfirmEmailAsync(
        string normalizedEmail,
        string emailConfirmationTokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Trial local identity requires ArchLucid:StorageProvider=Sql and migration 077 (dbo.IdentityUsers).");
    }

    /// <inheritdoc />
    public Task RecordAccessFailedAsync(
        string normalizedEmail,
        int newCount,
        DateTimeOffset? lockoutEnd,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetAccessFailedAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
