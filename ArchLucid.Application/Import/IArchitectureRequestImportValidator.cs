using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Import;

/// <summary>FluentValidation-backed import checks for <see cref="ArchitectureRequest" /> (API registers <c>FluentArchitectureRequestImportValidator</c>).</summary>
public interface IArchitectureRequestImportValidator
{
    Task<ArchitectureRequestImportValidationResult> ValidateAsync(ArchitectureRequest request, CancellationToken ct);
}
