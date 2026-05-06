using ArchLucid.Core.Configuration;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Identity;
/// <summary>NIST SP 800-63B aligned: length bounds only (no composition rules).</summary>
public sealed class TrialPasswordPolicyValidator(IOptions<TrialAuthOptions> trialOptions)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(trialOptions);
    private static byte __ValidatePrimaryConstructorArguments(Microsoft.Extensions.Options.IOptions<ArchLucid.Core.Configuration.TrialAuthOptions> trialOptions)
    {
        ArgumentNullException.ThrowIfNull(trialOptions);
        return (byte)0;
    }

    private readonly TrialAuthOptions _trial = trialOptions.Value ?? throw new ArgumentNullException(nameof(trialOptions));
    public TrialPasswordValidationResult Validate(string? password)
    {
        if (password is null)
            return TrialPasswordValidationResult.Fail("Password is required.");
        TrialLocalIdentityOptions local = _trial.LocalIdentity;
        if (password.Length < local.MinimumPasswordLength)
            return TrialPasswordValidationResult.Fail($"Password must be at least {local.MinimumPasswordLength} characters.");
        if (password.Length > local.MaximumPasswordLength)
            return TrialPasswordValidationResult.Fail($"Password must be at most {local.MaximumPasswordLength} characters.");
        return TrialPasswordValidationResult.Valid();
    }
}

public readonly struct TrialPasswordValidationResult
{
    private TrialPasswordValidationResult(bool ok, string? error)
    {
        Ok = ok;
        ErrorMessage = error;
    }

    public bool Ok { get; }
    public string? ErrorMessage { get; }

    public static TrialPasswordValidationResult Valid()
    {
        return new TrialPasswordValidationResult(true, null);
    }

    public static TrialPasswordValidationResult Fail(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new TrialPasswordValidationResult(false, message);
    }
}