using ArchiForge.Decisioning.Alerts.Simulation;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>
/// FluentValidation for <see cref="RuleSimulationRequest"/> (<c>POST …/alert-simulation/simulate</c>).
/// </summary>
/// <remarks>
/// Ensures <see cref="RuleSimulationRequest.RuleKind"/> is <c>Simple</c> or <c>Composite</c>; pairing with the correct rule body is enforced at runtime in <see cref="IRuleSimulationService"/>.
/// </remarks>
public sealed class RuleSimulationRequestValidator : AbstractValidator<RuleSimulationRequest>
{
    /// <summary>Requires a non-empty rule kind in the allowed set.</summary>
    public RuleSimulationRequestValidator()
    {
        RuleFor(x => x.RuleKind)
            .NotEmpty()
            .Must(k => k.Equals("Simple", StringComparison.OrdinalIgnoreCase) ||
                       k.Equals("Composite", StringComparison.OrdinalIgnoreCase))
            .WithMessage("RuleKind must be 'Simple' or 'Composite'.");
    }
}
