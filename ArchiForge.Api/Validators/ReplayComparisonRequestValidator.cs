using ArchiForge.Api.Models;
using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class ReplayComparisonRequestValidator : AbstractValidator<ReplayComparisonRequest>
{
    private static readonly HashSet<string> ValidFormats =
        new(StringComparer.OrdinalIgnoreCase) { "markdown", "html", "docx", "json" };

    private static readonly HashSet<string> ValidReplayModes =
        new(StringComparer.OrdinalIgnoreCase) { "artifact", "regenerate", "verify" };

    private static readonly HashSet<string> ValidProfiles =
        new(StringComparer.OrdinalIgnoreCase) { "default", "short", "detailed", "executive" };

    public ReplayComparisonRequestValidator()
    {
        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Format is required.")
            .Must(f => ValidFormats.Contains(f ?? "")).WithMessage("Format must be one of: markdown, html, docx, json.");

        RuleFor(x => x.ReplayMode)
            .NotEmpty().WithMessage("ReplayMode is required.")
            .Must(m => ValidReplayModes.Contains(m ?? "")).WithMessage("ReplayMode must be one of: artifact, regenerate, verify.");

        When(x => !string.IsNullOrWhiteSpace(x.Profile), () =>
        {
            RuleFor(x => x.Profile!)
                .Must(p => ValidProfiles.Contains(p.Trim())).WithMessage("Profile must be one of: default, short, detailed, executive.");
        });
    }
}
