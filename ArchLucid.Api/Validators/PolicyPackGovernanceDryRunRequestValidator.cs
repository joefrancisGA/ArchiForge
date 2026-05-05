using ArchLucid.Api.Models;

using FluentValidation;

namespace ArchLucid.Api.Validators;

/// <summary>
///     FluentValidation for <see cref="PolicyPackGovernanceDryRunRequest"/> (
///     <c>POST /v1/governance/policy-packs/dry-run</c>).
/// </summary>
public sealed class PolicyPackGovernanceDryRunRequestValidator : AbstractValidator<PolicyPackGovernanceDryRunRequest>
{
    /// <summary>Registers JSON, XOR target, and severity ordinal rules.</summary>
    public PolicyPackGovernanceDryRunRequestValidator()
    {
        RuleFor(x => x.PolicyPackContentJson)
            .NotEmpty()
            .WithMessage("PolicyPackContentJson is required.")
            .Must(PolicyPackRequestValidationRules.BeValidJson)
            .WithMessage("PolicyPackContentJson must be valid JSON.");

        RuleFor(x => x)
            .Must(x =>
                (!string.IsNullOrWhiteSpace(x.TargetRunId) && x.TargetManifestId is null) ||
                (string.IsNullOrWhiteSpace(x.TargetRunId) && x.TargetManifestId is not null))
            .WithMessage("Specify exactly one of targetRunId or targetManifestId.");

        RuleFor(x => x.BlockCommitMinimumSeverity)
            .InclusiveBetween(0, 3)
            .When(x => x.BlockCommitMinimumSeverity.HasValue)
            .WithMessage("BlockCommitMinimumSeverity must be between 0 (Info) and 3 (Critical).");
    }
}
