using ArchiForge.Decisioning.Alerts;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>Validates alert rule JSON on create (tenant/workspace/project are stamped server-side by <c>AlertRulesController</c>).</summary>
public sealed class AlertRuleBodyValidator : AbstractValidator<AlertRule>
{
    /// <summary>Requires name, <see cref="AlertRule.RuleType"/>, and <see cref="AlertRule.Severity"/>.</summary>
    public AlertRuleBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.RuleType).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty();
    }
}
