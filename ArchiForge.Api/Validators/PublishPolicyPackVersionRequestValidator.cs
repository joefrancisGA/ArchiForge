using ArchiForge.Api.Controllers;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>
/// FluentValidation rules for <see cref="PublishPolicyPackVersionRequest"/> (<c>POST …/policy-packs/{id}/publish</c>).
/// </summary>
/// <remarks>
/// Ensures version label matches the same SemVer-style rules as assign, and <c>ContentJson</c> is parseable JSON before persistence.
/// </remarks>
public sealed class PublishPolicyPackVersionRequestValidator : AbstractValidator<PublishPolicyPackVersionRequest>
{
    /// <summary>Registers SemVer and JSON rules.</summary>
    public PublishPolicyPackVersionRequestValidator()
    {
        RuleFor(x => x.Version)
            .NotEmpty()
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Version is required.")
            .MaximumLength(50)
            .Must(PolicyPackRequestValidationRules.BePolicyPackSemVerVersion)
            .WithMessage("Version must be SemVer 2 style (e.g. 1.0.0, 2.1.0-rc.1, optional leading 'v').");

        RuleFor(x => x.ContentJson)
            .Must(PolicyPackRequestValidationRules.BeValidJson)
            .WithMessage("ContentJson must be valid JSON.");
    }
}
