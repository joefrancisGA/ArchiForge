using ArchiForge.Decisioning.Alerts;
using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>Validates alert rule JSON on create (tenant/workspace are stamped server-side).</summary>
public sealed class AlertRuleBodyValidator : AbstractValidator<AlertRule>
{
    public AlertRuleBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.RuleType).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty();
    }
}
