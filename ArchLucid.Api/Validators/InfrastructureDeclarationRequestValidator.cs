using ArchiForge.Contracts.Requests;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class InfrastructureDeclarationRequestValidator : AbstractValidator<InfrastructureDeclarationRequest>
{
    public static readonly string[] SupportedFormats =
    [
        "json",
        "simple-terraform"
    ];

    public InfrastructureDeclarationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Infrastructure declaration Name is required.")
            .MaximumLength(500).WithMessage("Infrastructure declaration Name must not exceed 500 characters.");

        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Infrastructure declaration Format is required.")
            .MaximumLength(64)
            .Must(f => SupportedFormats.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Format must be 'json' or 'simple-terraform'.");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("Infrastructure declaration Content must not be null.")
            .MaximumLength(2_000_000).WithMessage("Infrastructure declaration Content must not exceed 2000000 characters.");
    }
}
