using ArchiForge.Api.Configuration;
using ArchiForge.Api.Models;

using FluentValidation;

using Microsoft.Extensions.Options;

namespace ArchiForge.Api.Validators;

public sealed class BatchReplayComparisonRequestValidator : AbstractValidator<BatchReplayComparisonRequest>
{
    public BatchReplayComparisonRequestValidator(IOptionsMonitor<BatchReplayOptions> batchOptions)
    {
        RuleFor(x => x.ComparisonRecordIds)
            .NotEmpty().WithMessage("At least one comparison record ID is required.")
            .Must(ids => ids == null || ids.All(id => !string.IsNullOrWhiteSpace(id)))
                .WithMessage("comparisonRecordIds must not contain blank or whitespace-only entries.")
            .Must(ids => ids is null || ids.Count <= batchOptions.CurrentValue.MaxComparisonRecordIds)
                .WithMessage(_ =>
                    $"comparisonRecordIds may contain at most {batchOptions.CurrentValue.MaxComparisonRecordIds} entries.");

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
