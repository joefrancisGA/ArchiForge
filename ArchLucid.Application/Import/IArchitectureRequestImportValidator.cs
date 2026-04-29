using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Import;

/// <summary>FluentValidation-backed import checks for <see cref="ArchitectureRequest" /> (implemented in API host).</summary>
public interface IArchitectureRequestImportValidator
{
    Task<ArchitectureRequestImportValidationResult> ValidateAsync(ArchitectureRequest request, CancellationToken ct);
}
