using ArchiForge.Decisioning.Alerts.Composite;
using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class CompositeAlertRuleBodyValidator : AbstractValidator<CompositeAlertRule>
{
    public CompositeAlertRuleBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty();
        RuleFor(x => x.Operator).NotEmpty();
        RuleFor(x => x.Conditions).NotEmpty().WithMessage("At least one condition is required.");
    }
}
