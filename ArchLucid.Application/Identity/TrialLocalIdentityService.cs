using System.Security.Cryptography;

using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Identity;

public sealed class TrialLocalIdentityService(
    IOptions<TrialAuthOptions>? trialOptions,
    ITrialIdentityUserRepository repository,
    PasswordHasher<TrialIdentityHasherUser> passwordHasher,
    TrialPasswordPolicyValidator passwordPolicy,
    PwnedPasswordRangeClient pwnedClient) : ITrialLocalIdentityService
{
    private readonly PasswordHasher<TrialIdentityHasherUser> _passwordHasher =
        passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

    private readonly TrialPasswordPolicyValidator _passwordPolicy =
        passwordPolicy ?? throw new ArgumentNullException(nameof(passwordPolicy));

    private readonly PwnedPasswordRangeClient _pwnedClient =
        pwnedClient ?? throw new ArgumentNullException(nameof(pwnedClient));

    private readonly ITrialIdentityUserRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly TrialAuthOptions _trial =
        trialOptions?.Value ?? throw new ArgumentNullException(nameof(trialOptions));

    /// <inheritdoc />
    public async Task<TrialLocalRegistrationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        EnsureLocalIdentityEnabled();

        string normalized = TrialEmailNormalizer.Normalize(email);
        TrialIdentityUserRecord? existing = await _repository.GetByNormalizedEmailAsync(normalized, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        TrialPasswordValidationResult policy = _passwordPolicy.Validate(password);

        if (!policy.Ok)
            throw new ArgumentException(policy.ErrorMessage ?? "Invalid password.", nameof(password));

        if (await _pwnedClient.IsPasswordPwnedAsync(password, cancellationToken))
            throw new ArgumentException("This password appears in public breach datasets; choose another.");

        string hash = _passwordHasher.HashPassword(new TrialIdentityHasherUser(), password);
        string securityStamp = Guid.NewGuid().ToString("N");
        string concurrencyStamp = Guid.NewGuid().ToString("N");
        string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        string tokenHash = TrialEmailVerificationTokenHasher.Hash(rawToken);
        DateTimeOffset expires = DateTimeOffset.UtcNow.AddDays(2);

        Guid userId = await _repository.CreatePendingUserAsync(
            normalized,
            email.Trim(),
            hash,
            securityStamp,
            concurrencyStamp,
            tokenHash,
            expires,
            cancellationToken);

        return new TrialLocalRegistrationResult { UserId = userId, VerificationToken = rawToken };
    }

    /// <inheritdoc />
    public async Task<bool> VerifyEmailAsync(string email, string rawToken, CancellationToken cancellationToken)
    {
        EnsureLocalIdentityEnabled();

        if (string.IsNullOrWhiteSpace(rawToken))
            return false;

        string normalized = TrialEmailNormalizer.Normalize(email);
        string tokenHash = TrialEmailVerificationTokenHasher.Hash(rawToken);

        return await _repository.TryConfirmEmailAsync(normalized, tokenHash, DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TrialLocalAuthResult?> AuthenticateAsync(string email, string password,
        CancellationToken cancellationToken)
    {
        EnsureLocalIdentityEnabled();

        string normalized = TrialEmailNormalizer.Normalize(email);
        TrialIdentityUserRecord? row = await _repository.GetByNormalizedEmailAsync(normalized, cancellationToken);

        if (row is null)
            return null;

        if (row is { LockoutEnabled: true, LockoutEnd: { } le } && le > DateTimeOffset.UtcNow)
            return null;

        PasswordVerificationResult verify =
            _passwordHasher.VerifyHashedPassword(new TrialIdentityHasherUser(), row.PasswordHash, password);

        if (verify != PasswordVerificationResult.Success && verify != PasswordVerificationResult.SuccessRehashNeeded)
        {
            int fails = row.AccessFailedCount + 1;
            DateTimeOffset? lockoutEnd = null;

            if (fails >= _trial.LocalIdentity.MaxFailedAccessAttemptsBeforeLockout)

                lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(_trial.LocalIdentity.LockoutMinutes);

            await _repository.RecordAccessFailedAsync(normalized, fails, lockoutEnd, cancellationToken);

            return null;
        }

        await _repository.ResetAccessFailedAsync(normalized, cancellationToken);

        return row.EmailVerifiedUtc is null
            ? null
            : new TrialLocalAuthResult { UserId = row.Id, Email = row.Email, Role = ArchLucidRoles.Reader };
    }

    private void EnsureLocalIdentityEnabled()
    {
        if (!TrialAuthModeConstants.HasMode(_trial.Modes, TrialAuthModeConstants.LocalIdentity))
            throw new InvalidOperationException("Auth:Trial:Modes does not include LocalIdentity.");
    }
}
