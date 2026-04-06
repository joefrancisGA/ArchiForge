using ArchiForge.ContextIngestion;
using ArchiForge.Contracts.Requests;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>
/// FluentValidation rules for <see cref="ContextDocumentRequest"/> items embedded in an
/// <see cref="ArchiForge.Contracts.Requests.ArchitectureRequest.Documents"/> collection.
/// Validates the document name, supported content type, and content size.
/// Name and ContentType reject whitespace-only values (FluentValidation <c>NotEmpty()</c> allows spaces).
/// Content types must match <see cref="SupportedContextDocumentContentTypes.IsSupported"/> (same rule as parsers).
/// </summary>
public sealed class ContextDocumentRequestValidator : AbstractValidator<ContextDocumentRequest>
{
    public ContextDocumentRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Document Name is required and cannot be whitespace-only.")
            .MaximumLength(500).WithMessage("Document Name must not exceed 500 characters.");

        RuleFor(x => x.ContentType)
            .Must(ct => !string.IsNullOrWhiteSpace(ct))
            .WithMessage("Document ContentType is required and cannot be whitespace-only.")
            .MaximumLength(255).WithMessage("Document ContentType must not exceed 255 characters.")
            .Must(SupportedContextDocumentContentTypes.IsSupported)
            .WithMessage(
                _ =>
                    "Document ContentType must be a supported MIME type: "
                    + string.Join(", ", SupportedContextDocumentContentTypes.All)
                    + ".");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("Document Content must not be null.")
            .MaximumLength(500_000).WithMessage("Document Content must not exceed 500000 characters.");
    }
}
