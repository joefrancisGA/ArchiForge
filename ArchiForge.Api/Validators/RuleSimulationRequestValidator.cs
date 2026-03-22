using ArchiForge.Decisioning.Alerts.Simulation;
using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class RuleSimulationRequestValidator : AbstractValidator<RuleSimulationRequest>
{
    public RuleSimulationRequestValidator()
    {
        RuleFor(x => x.RuleKind)
            .NotEmpty()
            .Must(k => k.Equals("Simple", StringComparison.OrdinalIgnoreCase) ||
                       k.Equals("Composite", StringComparison.OrdinalIgnoreCase))
            .WithMessage("RuleKind must be 'Simple' or 'Composite'.");
    }
}
