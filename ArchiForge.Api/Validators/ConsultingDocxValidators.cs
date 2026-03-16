using ArchiForge.Api.Models;
using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class ConsultingDocxExportRequestValidator : AbstractValidator<ConsultingDocxExportRequest>
{
    public ConsultingDocxExportRequestValidator()
    {
        RuleFor(x => x.TemplateProfile)
            .MaximumLength(100).WithMessage("TemplateProfile must not exceed 100 characters.");

        RuleFor(x => x.Audience)
            .MaximumLength(500).WithMessage("Audience must not exceed 500 characters.");

        RuleFor(x => x.DeterminismIterations)
            .InclusiveBetween(1, 20)
            .WithMessage("DeterminismIterations must be between 1 and 20.");

        When(x => x.IncludeManifestCompare, () =>
        {
            RuleFor(x => x.CompareManifestVersion)
                .NotEmpty().WithMessage("CompareManifestVersion is required when IncludeManifestCompare is true.")
                .MaximumLength(200).WithMessage("CompareManifestVersion must not exceed 200 characters.");
        });

        When(x => x.IncludeAgentResultCompare, () =>
        {
            RuleFor(x => x.CompareRunId)
                .NotEmpty().WithMessage("CompareRunId is required when IncludeAgentResultCompare is true.")
                .MaximumLength(200).WithMessage("CompareRunId must not exceed 200 characters.");
        });
    }
}

public sealed class ConsultingDocxProfileRecommendationRequestValidator
    : AbstractValidator<ConsultingDocxProfileRecommendationRequest>
{
    public ConsultingDocxProfileRecommendationRequestValidator()
    {
        RuleFor(x => x.Audience)
            .MaximumLength(500).WithMessage("Audience must not exceed 500 characters.");
    }
}

