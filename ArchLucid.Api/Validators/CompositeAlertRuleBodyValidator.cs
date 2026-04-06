using ArchiForge.Decisioning.Alerts.Composite;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>
/// FluentValidation for <see cref="CompositeAlertRule"/> bodies on <c>POST …/composite-alert-rules</c> (scope ids stamped server-side).
/// </summary>
public sealed class CompositeAlertRuleBodyValidator : AbstractValidator<CompositeAlertRule>
{
    /// <summary>Requires name, severity, operator, and at least one <see cref="CompositeAlertRule.Conditions"/> entry.</summary>
    public CompositeAlertRuleBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty();
        RuleFor(x => x.Operator).NotEmpty();
        RuleFor(x => x.Conditions).NotEmpty().WithMessage("At least one condition is required.");
    }
}
