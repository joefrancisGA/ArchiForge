using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Decisioning.Validation;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Offline <c>archlucid manifest validate --file &lt;path&gt;</c>: validates against the bundled Golden Manifest JSON Schema
///     using the same strict JSON Schema evaluator as <see cref="SchemaValidationService" /> in ArchLucid.Decisioning.
/// </summary>
internal static class ManifestValidateCommand
{
    private static readonly JsonSerializerOptions SJsonCamel =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    ///     CI contract per product doc: <c>0</c> success, <c>1</c> any failure (matches <see cref="CliExitCode.UsageError" />).
    /// </summary>
    private const int FailureExitCode = CliExitCode.UsageError;

    internal static Task<int> RunAsync(string[] args)
    {
        if (!TryResolveManifestFilePath(args, out string? filePath, out string? parseError))
        {
            EmitUsage(parseError);

            return Task.FromResult(FailureExitCode);
        }

        string absoluteManifestPath = Path.GetFullPath(filePath!.Trim());
        ManifestValidateOutcome outcome =
            GoldenManifestOfflineSchemaValidator.ValidateManifestFile(absoluteManifestPath);

        if (outcome.IsValid)
        {
            EmitSuccess(absoluteManifestPath);

            return Task.FromResult(CliExitCode.Success);
        }

        EmitFailures(absoluteManifestPath, outcome);

        return Task.FromResult(FailureExitCode);
    }

    private static void EmitSuccess(string absoluteManifestPath)
    {
        if (CliExecutionContext.JsonOutput)
        {
            Console.WriteLine(
                JsonSerializer.Serialize(
                    new
                    {
                        ok = true,
                        manifest = absoluteManifestPath,
                        schema = "schemas/goldenmanifest.schema.json"
                    },
                    SJsonCamel));

            return;
        }

        Console.WriteLine($"Valid golden manifest: {absoluteManifestPath}");
    }

    private static void EmitFailures(string absoluteManifestPath, ManifestValidateOutcome outcome)
    {
        if (CliExecutionContext.JsonOutput)
        {
            object payload = new
            {
                ok = false,
                exitCode = FailureExitCode,
                error = "manifest_validate",
                manifest = absoluteManifestPath,
                violations = outcome.Errors
                    .Select(e => new
                    {
                        message = e.Message,
                        line = e.LineNumber,
                        column = e.Column,
                        instancePointer = e.InstancePointer
                    })
                    .ToArray()
            };

            Console.Error.WriteLine(JsonSerializer.Serialize(payload, SJsonCamel));

            return;
        }

        Console.Error.WriteLine($"[manifest validate] {outcome.Errors.Count} issue(s) in {absoluteManifestPath}");

        foreach (ManifestValidateError err in outcome.Errors)
        {
            string where = FormatWhere(err);

            Console.Error.WriteLine($"  - {err.Message}{where}");
        }
    }

    private static string FormatWhere(ManifestValidateError err)
    {
        if (err.LineNumber is int line && err.Column is int col)
        {
            string pointer = err.InstancePointer is { Length: > 0 } p ? $" at {p}" : string.Empty;

            return $" (line {line}, column {col}{pointer})";
        }

        if (err.InstancePointer is { Length: > 0 } onlyPointer)
            return $" ({onlyPointer})";

        return string.Empty;
    }

    private static void EmitUsage(string? detail)
    {
        const string usage = "Usage: archlucid manifest validate --file <path-to.json> (alias: -f)";

        if (CliExecutionContext.JsonOutput)
        {
            string message = detail is null ? usage : $"{usage} {detail}";

            CliJson.WriteFailureLine(Console.Error, FailureExitCode, "manifest_validate", message);

            return;
        }

        Console.Error.WriteLine(usage);

        if (detail is not null)
            Console.Error.WriteLine(detail);
    }

    private static bool TryResolveManifestFilePath(string[] args, [NotNullWhen(true)] out string? filePath,
        out string? error)
    {
        filePath = null;
        error = null;
        string? resolved = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.Length == 0)
                continue;

            if (arg.StartsWith("--file=", StringComparison.Ordinal))
            {
                string value = arg["--file=".Length..].Trim();

                if (value.Length == 0)
                {
                    error = "Missing value after --file=.";

                    return false;
                }

                if (resolved is not null)
                {
                    error = "Only one manifest path may be specified.";

                    return false;
                }

                resolved = value;

                continue;
            }

            if (string.Equals(arg, "--file", StringComparison.Ordinal)
                || string.Equals(arg, "-f", StringComparison.Ordinal))
            {
                if (i + 1 >= args.Length)
                {
                    error = $"Missing path after {arg}.";

                    return false;
                }

                if (resolved is not null)
                {
                    error = "Only one manifest path may be specified.";

                    return false;
                }

                resolved = args[++i].Trim();

                if (resolved.Length == 0)
                {
                    error = "Manifest path is empty.";

                    return false;
                }

                continue;
            }

            error = $"Unexpected argument '{arg}'.";

            return false;
        }

        if (resolved is null)
        {
            error = "Missing required --file <path>.";

            return false;
        }

        filePath = resolved;

        return true;
    }
}
