using ArchiForge.Api.Controllers;
using ArchiForge.Decisioning.Governance.Resolution;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>
/// FluentValidation rules for <see cref="AssignPolicyPackRequest"/> (<c>POST …/policy-packs/{id}/assign</c>).
/// </summary>
/// <remarks>
/// Complements domain checks in <see cref="Services.IPolicyPacksAppService.TryAssignAsync"/> (version row existence → 404):
/// this validator only guards shape and known <see cref="GovernanceScopeLevel"/> values before the app service runs.
/// </remarks>
public sealed class AssignPolicyPackRequestValidator : AbstractValidator<AssignPolicyPackRequest>
{
    /// <summary>Registers SemVer for <c>Version</c> and whitelist for <c>ScopeLevel</c> via <see cref="GovernanceScopeLevel.TryNormalize"/>.</summary>
    public AssignPolicyPackRequestValidator()
    {
        RuleFor(x => x.Version)
            .NotEmpty()
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Version is required.")
            .MaximumLength(50)
            .Must(PolicyPackRequestValidationRules.BePolicyPackSemVerVersion)
            .WithMessage("Version must be SemVer 2 style (e.g. 1.0.0, 2.1.0-rc.1, optional leading 'v').");

        RuleFor(x => x.ScopeLevel)
            .Must(sl => GovernanceScopeLevel.TryNormalize(sl) is not null)
            .WithMessage($"ScopeLevel must be one of: {string.Join(", ", GovernanceScopeLevel.All)}.");
    }
}
