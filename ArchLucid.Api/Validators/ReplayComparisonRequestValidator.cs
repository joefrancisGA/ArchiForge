using ArchiForge.Api.Models;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class ReplayComparisonRequestValidator : AbstractValidator<ReplayComparisonRequest>
{
    public ReplayComparisonRequestValidator()
    {
        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Format is required.")
            .Must(f => ReplayValidationConstants.ValidFormats.Contains(f ?? "")).WithMessage("Format must be one of: markdown, html, docx, json.");

        RuleFor(x => x.ReplayMode)
            .NotEmpty().WithMessage("ReplayMode is required.")
            .Must(m => ReplayValidationConstants.ValidReplayModes.Contains(m ?? "")).WithMessage("ReplayMode must be one of: artifact, regenerate, verify.");

        When(x => !string.IsNullOrWhiteSpace(x.Profile), () =>
        {
            RuleFor(x => x.Profile!)
                .Must(p => ReplayValidationConstants.ValidProfiles.Contains(p.Trim())).WithMessage("Profile must be one of: default, short, detailed, executive.");
        });
    }
}
