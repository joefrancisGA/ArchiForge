using ArchiForge.Api.Models;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class ComparisonHistoryQueryValidator : AbstractValidator<ComparisonHistoryQuery>
{
    public ComparisonHistoryQueryValidator()
    {
        RuleFor(x => x.ComparisonType)
            .Must(t => string.IsNullOrWhiteSpace(t)
                       || string.Equals(t.Trim(), "end-to-end-replay", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(t.Trim(), "export-record-diff", StringComparison.OrdinalIgnoreCase))
            .WithMessage("comparisonType must be empty, 'end-to-end-replay', or 'export-record-diff'.");

        RuleFor(x => x)
            .Must(q => q.CreatedFromUtc is null || q.CreatedToUtc is null || q.CreatedFromUtc <= q.CreatedToUtc)
            .WithMessage("createdFromUtc must be <= createdToUtc.");

        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).WithMessage("skip must be >= 0.");

        RuleFor(x => x.Limit).InclusiveBetween(0, 500).WithMessage("limit must be between 0 and 500 (0 = default 50).");

        RuleFor(x => x.SortDir)
            .Must(d => string.IsNullOrWhiteSpace(d)
                       || string.Equals(d, "asc", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(d, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("sortDir must be 'asc' or 'desc'.");

        RuleFor(x => x.SortBy)
            .Must(s => string.IsNullOrWhiteSpace(s)
                       || string.Equals(s, "createdUtc", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(s, "type", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(s, "label", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(s, "leftRunId", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(s, "rightRunId", StringComparison.OrdinalIgnoreCase))
            .WithMessage("sortBy must be one of: createdUtc, type, label, leftRunId, rightRunId.");

        RuleFor(x => x)
            .Must(q => string.IsNullOrWhiteSpace(q.Cursor)
                       || string.Equals(q.SortBy ?? "createdUtc", "createdUtc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("cursor paging currently requires sortBy=createdUtc.");
    }
}
