using ArchLucid.Decisioning.Validation;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class SchemaValidationRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        SchemaValidationOptions opts =
            configuration.GetSection(SchemaValidationOptions.SectionName).Get<SchemaValidationOptions>()
            ?? new SchemaValidationOptions();

        string baseDir = AppContext.BaseDirectory;
        ValidateSchemaPath(opts.AgentResultSchemaPath, "AgentResult", baseDir, errors);
        ValidateSchemaPath(opts.GoldenManifestSchemaPath, "GoldenManifest", baseDir, errors);
    }

    private static void ValidateSchemaPath(
        string relativePath,
        string logicalName,
        string baseDir,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            errors.Add($"SchemaValidation schema path for {logicalName} is missing or empty.");

            return;
        }

        string trimmed = relativePath.Trim();

        if (Path.IsPathRooted(trimmed))
        {
            errors.Add(
                $"SchemaValidation path for {logicalName} must be relative to the application base directory, not an absolute path (got '{trimmed}').");

            return;
        }

        string normalizedBase = Path.GetFullPath(baseDir);
        string fullPath = Path.GetFullPath(Path.Combine(baseDir, trimmed));
        string relativeToBase = Path.GetRelativePath(normalizedBase, fullPath);

        if (relativeToBase.Equals("..", StringComparison.Ordinal) ||
            relativeToBase.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relativeToBase.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            errors.Add(
                $"SchemaValidation path for {logicalName} escapes the application base directory (got '{trimmed}').");

            return;
        }

        if (!File.Exists(fullPath))

            errors.Add(
                $"Schema file for {logicalName} was not found at '{fullPath}' (SchemaValidation:*SchemaPath). Ensure content is copied to output (e.g. schemas in project output).");
    }
}
