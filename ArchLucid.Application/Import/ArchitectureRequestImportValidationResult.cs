namespace ArchLucid.Application.Import;

/// <summary>Normalized validation outcome for request-file import (HTTP 422 mapping lives in the controller).</summary>
public sealed class ArchitectureRequestImportValidationResult
{
    public bool IsValid
    {
        get;
        init;
    }

    public IReadOnlyList<string> Errors
    {
        get;
        init;
    } = [];
}
