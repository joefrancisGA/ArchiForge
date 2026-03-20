using ArchiForge.Contracts.Requests;
using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class ContextDocumentRequestValidator : AbstractValidator<ContextDocumentRequest>
{
    public static readonly string[] SupportedContentTypes =
    [
        "text/plain",
        "text/markdown"
    ];

    public ContextDocumentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Document Name is required.")
            .MaximumLength(500).WithMessage("Document Name must not exceed 500 characters.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Document ContentType is required.")
            .MaximumLength(255)
            .Must(ct => SupportedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                "Document ContentType must be a supported type (e.g. text/plain, text/markdown).");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("Document Content must not be null.")
            .MaximumLength(500_000).WithMessage("Document Content must not exceed 500000 characters.");
    }
}
