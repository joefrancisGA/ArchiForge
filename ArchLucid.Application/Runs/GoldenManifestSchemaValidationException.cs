using ArchLucid.Decisioning.Validation;

namespace ArchLucid.Application.Runs;

/// <summary>Thrown when the projected golden manifest fails JSON Schema validation before persistence.</summary>
public sealed class GoldenManifestSchemaValidationException : Exception
{
    public GoldenManifestSchemaValidationException(SchemaValidationResult result)
        : base(BuildMessage(result))
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public SchemaValidationResult Result
    {
        get;
    }

    private static string BuildMessage(SchemaValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Errors.Count == 0)
            return "Golden manifest failed schema validation.";

        return "Golden manifest failed schema validation: " + string.Join("; ", result.Errors);
    }
}
