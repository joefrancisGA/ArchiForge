using ArchiForge.Application.Analysis;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class ArchitectureAnalysisRequestValidator : AbstractValidator<ArchitectureAnalysisRequest>
{
    public ArchitectureAnalysisRequestValidator()
    {
        // RunId is often supplied from the route after binding; API actions assign it before calling the app layer.
        RuleFor(x => x.RunId)
            .MaximumLength(64)
            .WithMessage("RunId must not exceed 64 characters.");

        RuleFor(x => x.DeterminismIterations)
            .InclusiveBetween(2, 20)
            .WithMessage("DeterminismIterations must be between 2 and 20 when IncludeDeterminismCheck is true.")
            .When(x => x.IncludeDeterminismCheck);

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
