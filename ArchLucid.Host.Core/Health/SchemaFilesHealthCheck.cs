using ArchiForge.Decisioning.Validation;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Core.Health;

/// <summary>
/// Verifies JSON Schema files for agent results and golden manifests exist where <see cref="SchemaValidationService"/> loads them.
/// </summary>
public sealed class SchemaFilesHealthCheck(IOptions<SchemaValidationOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        SchemaValidationOptions opts = options.Value;
        List<string> problems = [];

        AddProblemsForPath(opts.AgentResultSchemaPath, "AgentResult", problems);
        AddProblemsForPath(opts.GoldenManifestSchemaPath, "GoldenManifest", problems);

        if (problems.Count > 0)
        
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "One or more schema files are missing or misconfigured: " + string.Join("; ", problems)));
        

        return Task.FromResult(
            HealthCheckResult.Healthy(
                $"Schema files present: {opts.AgentResultSchemaPath}, {opts.GoldenManifestSchemaPath}."));
    }

    private static void AddProblemsForPath(string relativePath, string logicalName, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            problems.Add($"{logicalName} schema path is empty (SchemaValidation section).");

            return;
        }

        string trimmed = relativePath.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            problems.Add($"{logicalName} schema path must be relative (got rooted path).");

            return;
        }

        string baseDir = AppContext.BaseDirectory;
        string normalizedBase = Path.GetFullPath(baseDir);
        string fullPath = Path.GetFullPath(Path.Combine(baseDir, trimmed));
        string relativeToBase = Path.GetRelativePath(normalizedBase, fullPath);
        if (relativeToBase.Equals("..", StringComparison.Ordinal) ||
            relativeToBase.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relativeToBase.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            problems.Add($"{logicalName} schema path escapes the application base directory.");

            return;
        }

        if (!File.Exists(fullPath))
        
            problems.Add($"{logicalName} schema not found at '{fullPath}'.");
        
    }
}
