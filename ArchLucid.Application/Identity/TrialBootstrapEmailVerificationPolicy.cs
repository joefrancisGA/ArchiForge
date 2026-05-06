using ArchLucid.Core.Configuration;
using ArchLucid.Core.Identity;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Identity;
/// <inheritdoc cref = "ITrialBootstrapEmailVerificationPolicy"/>
/// <remarks>
///     When <c>LocalIdentity</c> is enabled and a row exists in <c>dbo.IdentityUsers</c> for the admin email,
///     <see cref = "ArchLucid.Core.Identity.TrialIdentityUserRecord.EmailVerifiedUtc"/> must be set before trial
///     provisioning runs.
/// </remarks>
public sealed class TrialBootstrapEmailVerificationPolicy(IOptions<TrialAuthOptions>? trialOptions, ITrialIdentityUserRepository identityUsers) : ITrialBootstrapEmailVerificationPolicy
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(trialOptions, identityUsers);
    private static byte __ValidatePrimaryConstructorArguments(Microsoft.Extensions.Options.IOptions<ArchLucid.Core.Configuration.TrialAuthOptions>? trialOptions, ArchLucid.Core.Identity.ITrialIdentityUserRepository identityUsers)
    {
        ArgumentNullException.ThrowIfNull(identityUsers);
        return (byte)0;
    }

    private readonly ITrialIdentityUserRepository _identityUsers = identityUsers ?? throw new ArgumentNullException(nameof(identityUsers));
    private readonly TrialAuthOptions _trial = trialOptions?.Value ?? throw new ArgumentNullException(nameof(trialOptions));
    /// <inheritdoc/>
    public async Task<bool> CanProvisionTrialForRegisteredEmailAsync(string adminEmail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adminEmail);
        if (string.IsNullOrWhiteSpace(adminEmail))
            return false;
        if (!TrialAuthModeConstants.HasMode(_trial.Modes, TrialAuthModeConstants.LocalIdentity))
            return true;
        string normalized = TrialEmailNormalizer.Normalize(adminEmail);
        TrialIdentityUserRecord? row = await _identityUsers.GetByNormalizedEmailAsync(normalized, cancellationToken);
        if (row is null)
            return true;
        return row.EmailVerifiedUtc is not null;
    }
}