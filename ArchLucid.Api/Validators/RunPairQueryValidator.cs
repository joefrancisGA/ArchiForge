using ArchiForge.Api.Models;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class RunPairQueryValidator : AbstractValidator<RunPairQuery>
{
    public RunPairQueryValidator()
    {
        RuleFor(x => x.LeftRunId)
            .NotEmpty()
            .WithMessage("leftRunId is required.");

        RuleFor(x => x.RightRunId)
            .NotEmpty()
            .WithMessage("rightRunId is required.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.LeftRunId, x.RightRunId, StringComparison.OrdinalIgnoreCase))
            .WithMessage("leftRunId and rightRunId must be different.");
    }
}
