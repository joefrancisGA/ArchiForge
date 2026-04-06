using ArchiForge.Api.Models;
using ArchiForge.Contracts.Governance;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class CreateGovernanceEnvironmentComparisonRequestValidator : AbstractValidator<CreateGovernanceEnvironmentComparisonRequest>
{
    private static readonly string[] ValidEnvironments =
        [GovernanceEnvironment.Dev, GovernanceEnvironment.Test, GovernanceEnvironment.Prod];

    public CreateGovernanceEnvironmentComparisonRequestValidator()
    {
        RuleFor(x => x.SourceEnvironment)
            .NotEmpty().WithMessage("SourceEnvironment is required.")
            .Must(e => ValidEnvironments.Contains(e, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SourceEnvironment must be one of: dev, test, prod.");

        RuleFor(x => x.TargetEnvironment)
            .NotEmpty().WithMessage("TargetEnvironment is required.")
            .Must(e => ValidEnvironments.Contains(e, StringComparer.OrdinalIgnoreCase))
            .WithMessage("TargetEnvironment must be one of: dev, test, prod.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.SourceEnvironment, x.TargetEnvironment, StringComparison.OrdinalIgnoreCase))
            .WithMessage("SourceEnvironment and TargetEnvironment must be different.")
            .When(x => !string.IsNullOrEmpty(x.SourceEnvironment) && !string.IsNullOrEmpty(x.TargetEnvironment));
    }
}
