using ArchiForge.Contracts.Requests;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>
/// FluentValidation rules for <see cref="ArchitectureRequest"/>.
/// Validates required fields, character limits, and collection cardinality before the
/// request is passed to the run-creation pipeline.
/// </summary>
public sealed class ArchitectureRequestValidator : AbstractValidator<ArchitectureRequest>
{
    public ArchitectureRequestValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.")
            .MaximumLength(64).WithMessage("RequestId must not exceed 64 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters.");

        RuleFor(x => x.SystemName)
            .NotEmpty().WithMessage("SystemName is required.")
            .MaximumLength(200).WithMessage("SystemName must not exceed 200 characters.");

        RuleFor(x => x.Environment)
            .NotEmpty().WithMessage("Environment is required.")
            .MaximumLength(50).WithMessage("Environment must not exceed 50 characters.");

        RuleFor(x => x.CloudProvider)
            .IsInEnum().WithMessage("CloudProvider must be a valid value.");

        RuleFor(x => x.Constraints)
            .NotNull().WithMessage("Constraints must not be null.")
            .Must(c => c.Count <= 50).WithMessage("Constraints must not exceed 50 items.");

        RuleFor(x => x.RequiredCapabilities)
            .NotNull().WithMessage("RequiredCapabilities must not be null.")
            .Must(c => c.Count <= 50).WithMessage("RequiredCapabilities must not exceed 50 items.");

        RuleFor(x => x.Assumptions)
            .NotNull().WithMessage("Assumptions must not be null.")
            .Must(c => c.Count <= 50).WithMessage("Assumptions must not exceed 50 items.");

        RuleFor(x => x.InlineRequirements)
            .NotNull().WithMessage("InlineRequirements must not be null.")
            .Must(c => c.Count <= 100).WithMessage("InlineRequirements must not exceed 100 items.");

        RuleForEach(x => x.InlineRequirements)
            .MaximumLength(4000).WithMessage("Each inline requirement must not exceed 4000 characters.");

        RuleFor(x => x.Documents)
            .NotNull().WithMessage("Documents must not be null.")
            .Must(c => c.Count <= 50).WithMessage("Documents must not exceed 50 items.");

        RuleForEach(x => x.Documents)
            .SetValidator(new ContextDocumentRequestValidator());

        RuleFor(x => x.PolicyReferences)
            .NotNull().WithMessage("PolicyReferences must not be null.")
            .Must(c => c.Count <= 100).WithMessage("PolicyReferences must not exceed 100 items.");

        RuleForEach(x => x.PolicyReferences)
            .MaximumLength(500).WithMessage("Each policy reference must not exceed 500 characters.");

        RuleFor(x => x.TopologyHints)
            .NotNull().WithMessage("TopologyHints must not be null.")
            .Must(c => c.Count <= 100).WithMessage("TopologyHints must not exceed 100 items.");

        RuleForEach(x => x.TopologyHints)
            .MaximumLength(2000).WithMessage("Each topology hint must not exceed 2000 characters.");

        RuleFor(x => x.SecurityBaselineHints)
            .NotNull().WithMessage("SecurityBaselineHints must not be null.")
            .Must(c => c.Count <= 100).WithMessage("SecurityBaselineHints must not exceed 100 items.");

        RuleForEach(x => x.SecurityBaselineHints)
            .MaximumLength(2000).WithMessage("Each security baseline hint must not exceed 2000 characters.");

        RuleFor(x => x.InfrastructureDeclarations)
            .NotNull().WithMessage("InfrastructureDeclarations must not be null.")
            .Must(c => c.Count <= 50).WithMessage("InfrastructureDeclarations must not exceed 50 items.");

        RuleForEach(x => x.InfrastructureDeclarations)
            .SetValidator(new InfrastructureDeclarationRequestValidator());
    }
}
