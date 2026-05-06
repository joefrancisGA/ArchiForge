using System.Security.Cryptography;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Identity;
public sealed class TrialLocalIdentityService(IOptions<TrialAuthOptions>? trialOptions, ITrialIdentityUserRepository repository, PasswordHasher<TrialIdentityHasherUser> passwordHasher, TrialPasswordPolicyValidator passwordPolicy, PwnedPasswordRangeClient pwnedClient, ITrialLocalIdentityAccountExistsNotifier accountExistsNotifier, ILogger<TrialLocalIdentityService> logger) : ITrialLocalIdentityService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(trialOptions, repository, passwordHasher, passwordPolicy, pwnedClient, accountExistsNotifier, logger);
    private static byte __ValidatePrimaryConstructorArguments(Microsoft.Extensions.Options.IOptions<ArchLucid.Core.Configuration.TrialAuthOptions>? trialOptions, ArchLucid.Core.Identity.ITrialIdentityUserRepository repository, Microsoft.AspNetCore.Identity.PasswordHasher<ArchLucid.Application.Identity.TrialIdentityHasherUser> passwordHasher, ArchLucid.Application.Identity.TrialPasswordPolicyValidator passwordPolicy, ArchLucid.Application.Identity.PwnedPasswordRangeClient pwnedClient, ArchLucid.Application.Identity.ITrialLocalIdentityAccountExistsNotifier accountExistsNotifier, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.Identity.TrialLocalIdentityService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(passwordPolicy);
        ArgumentNullException.ThrowIfNull(pwnedClient);
        ArgumentNullException.ThrowIfNull(accountExistsNotifier);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    // Fixed payload so failed lookups perform password hashing work comparable to the success path's verifier cost.
    private const string AuthenticationTimingDummyPassword = "Cw7qN9mK2pR4vL8xJ3hF6tY0zB5dS1gM";
    private readonly ITrialLocalIdentityAccountExistsNotifier _accountExistsNotifier = accountExistsNotifier ?? throw new ArgumentNullException(nameof(accountExistsNotifier));
    private readonly ILogger<TrialLocalIdentityService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly PasswordHasher<TrialIdentityHasherUser> _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    private readonly TrialPasswordPolicyValidator _passwordPolicy = passwordPolicy ?? throw new ArgumentNullException(nameof(passwordPolicy));
    private readonly PwnedPasswordRangeClient _pwnedClient = pwnedClient ?? throw new ArgumentNullException(nameof(pwnedClient));
    private readonly ITrialIdentityUserRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly TrialAuthOptions _trial = trialOptions?.Value ?? throw new ArgumentNullException(nameof(trialOptions));
    /// <inheritdoc/>
    public async Task<TrialLocalRegistrationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);
        EnsureLocalIdentityEnabled();
        string normalized = TrialEmailNormalizer.Normalize(email);
        TrialPasswordValidationResult policy = _passwordPolicy.Validate(password);
        if (!policy.Ok)
            throw new ArgumentException(policy.ErrorMessage ?? "Invalid password.", nameof(password));
        if (await _pwnedClient.IsPasswordPwnedAsync(password, cancellationToken))
            throw new ArgumentException("This password appears in public breach datasets; choose another.");
        TrialIdentityUserRecord? existing = await _repository.GetByNormalizedEmailAsync(normalized, cancellationToken);
        if (existing is not null)
        {
            QueueAccountAlreadyExistsNotice(email.Trim());
            return CreateOpaqueRegistrationResult();
        }

        string hash = _passwordHasher.HashPassword(new TrialIdentityHasherUser(), password);
        string securityStamp = Guid.NewGuid().ToString("N");
        string concurrencyStamp = Guid.NewGuid().ToString("N");
        string rawToken = CreateRawVerificationToken();
        string tokenHash = TrialEmailVerificationTokenHasher.Hash(rawToken);
        DateTimeOffset expires = DateTimeOffset.UtcNow.AddDays(2);
        Guid userId = await _repository.CreatePendingUserAsync(normalized, email.Trim(), hash, securityStamp, concurrencyStamp, tokenHash, expires, cancellationToken);
        return new TrialLocalRegistrationResult
        {
            UserId = userId,
            VerificationToken = rawToken
        };
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyEmailAsync(string email, string rawToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(rawToken);
        EnsureLocalIdentityEnabled();
        if (string.IsNullOrWhiteSpace(rawToken))
            return false;
        string normalized = TrialEmailNormalizer.Normalize(email);
        string tokenHash = TrialEmailVerificationTokenHasher.Hash(rawToken);
        return await _repository.TryConfirmEmailAsync(normalized, tokenHash, DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<ArchLucid.Application.Identity.TrialLocalAuthResult?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);
        EnsureLocalIdentityEnabled();
        string normalized = TrialEmailNormalizer.Normalize(email);
        TrialIdentityUserRecord? row = await _repository.GetByNormalizedEmailAsync(normalized, cancellationToken);
        if (row is null)
        {
            _passwordHasher.HashPassword(new TrialIdentityHasherUser(), AuthenticationTimingDummyPassword);
            return null;
        }

        if (row is { LockoutEnabled: true, LockoutEnd: { } le } && le > DateTimeOffset.UtcNow)
            return null;
        PasswordVerificationResult verify = _passwordHasher.VerifyHashedPassword(new TrialIdentityHasherUser(), row.PasswordHash, password);
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
        return row.EmailVerifiedUtc is null ? null : new TrialLocalAuthResult
        {
            UserId = row.Id,
            Email = row.Email,
            Role = ArchLucidRoles.Reader
        };
    }

    private void EnsureLocalIdentityEnabled()
    {
        if (!TrialAuthModeConstants.HasMode(_trial.Modes, TrialAuthModeConstants.LocalIdentity))
            throw new InvalidOperationException("Auth:Trial:Modes does not include LocalIdentity.");
    }

    private static string CreateRawVerificationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static TrialLocalRegistrationResult CreateOpaqueRegistrationResult()
    {
        return new TrialLocalRegistrationResult
        {
            UserId = Guid.NewGuid(),
            VerificationToken = CreateRawVerificationToken()
        };
    }

    private void QueueAccountAlreadyExistsNotice(string displayEmail)
    {
        Task task = SendAccountAlreadyExistsBestEffortAsync(displayEmail);
        _ = task;
    }

    private async Task SendAccountAlreadyExistsBestEffortAsync(string displayEmail)
    {
        try
        {
            await _accountExistsNotifier.NotifyAccountAlreadyExistsAsync(displayEmail, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(ex, "Failed to send trial local duplicate-registration notice for {Email}.", displayEmail);
        }
    }
}