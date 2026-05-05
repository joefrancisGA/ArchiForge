using ArchLucid.Decisioning.Validation;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArchLucid.Cli;

internal static class GoldenManifestOfflineSchemaValidator
{
    private static readonly JsonLoadSettings SLineTracking = new() { LineInfoHandling = LineInfoHandling.Load, CommentHandling = CommentHandling.Ignore };

    internal static ManifestValidateOutcome ValidateManifestFile(string manifestAbsolutePath)
    {
        ManifestValidateOutcome outcome = new();

        if (string.IsNullOrWhiteSpace(manifestAbsolutePath))
        {
            outcome.Errors.Add(new ManifestValidateError { Message = "Manifest path is required." });

            return outcome;
        }

        if (!File.Exists(manifestAbsolutePath))
        {
            outcome.Errors.Add(new ManifestValidateError { Message = $"File not found: {manifestAbsolutePath}" });

            return outcome;
        }

        string raw;

        try
        {
            raw = File.ReadAllText(manifestAbsolutePath);
        }
        catch (Exception ex)
        {
            outcome.Errors.Add(new ManifestValidateError { Message = $"Could not read file: {ex.Message}" });

            return outcome;
        }

        string trimmed = raw.TrimStart();

        if (trimmed.Length == 0)
        {
            outcome.Errors.Add(new ManifestValidateError { Message = "Manifest file is empty.", LineNumber = 1 });

            return outcome;
        }

        JToken manifestRoot;

        try
        {
            manifestRoot = JToken.Parse(raw, SLineTracking);
        }
        catch (JsonReaderException ex)
        {
            string? readerPath = ex.Path;

            outcome.Errors.Add(new ManifestValidateError
            {
                Message = $"Invalid JSON: {ex.Message}",
                LineNumber = TryPositiveLine(ex.LineNumber),
                Column = TryPositiveLine(ex.LinePosition),
                InstancePointer = readerPath is { Length: > 0 } ? readerPath : null
            });

            return outcome;
        }

        if (manifestRoot.Type is not JTokenType.Object)
        {
            AppendFirstTokenLineOutcome(outcome, manifestRoot,
                $"Expected a JSON object (golden manifest root). Found '{manifestRoot.Type}'.");

            return outcome;
        }

        SchemaValidationService service;

        try
        {
            service = CreateSchemaValidationService();
        }
        catch (Exception ex)
        {
            outcome.Errors.Add(new ManifestValidateError
            {
                Message =
                    $"Bundled Golden Manifest JSON Schema could not be loaded: {ex.Message}. " +
                    "Reinstall ArchLucid.Cli or ensure schemas/goldenmanifest.schema.json is beside the tool binaries."
            });

            return outcome;
        }

        SchemaValidationResult schemaResult = service.ValidateGoldenManifestJson(raw);

        if (schemaResult.IsValid)
            return outcome;

        AppendSchemaErrors(outcome, manifestRoot, schemaResult);

        return outcome;
    }

    private static void AppendSchemaErrors(ManifestValidateOutcome outcome, JToken manifestRoot,
        SchemaValidationResult schemaResult)
    {
        if (schemaResult.DetailedErrors.Count > 0)
        {
            foreach (SchemaValidationError detail in schemaResult.DetailedErrors)
            {
                string pointer = NormalizeInstancePointer(detail.Location);
                int? line = null;
                int? column = null;

                if (JsonPointerLineLocator.TryGetNewtonsoftSourceLine(manifestRoot, pointer, out int ln, out int col))
                {
                    line = ln;
                    column = col;
                }

                outcome.Errors.Add(new ManifestValidateError
                {
                    Message = detail.Message, LineNumber = line, Column = column, InstancePointer = pointer.Length > 0 ? pointer : null
                });
            }

            return;
        }

        foreach (string fallbackLine in schemaResult.Errors)
        {
            outcome.Errors.Add(new ManifestValidateError { Message = fallbackLine });
        }
    }

    private static void AppendFirstTokenLineOutcome(ManifestValidateOutcome outcome, JToken token, string message)
    {
        int? line = null;
        int? column = null;

        if (token is IJsonLineInfo li && li.HasLineInfo())
        {
            line = li.LineNumber;
            column = li.LinePosition;
        }

        outcome.Errors.Add(new ManifestValidateError { Message = message, LineNumber = line, Column = column });
    }

    private static string NormalizeInstancePointer(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return string.Empty;

        string trimmed = location.Trim();

        if (string.Equals(trimmed, "(root)", StringComparison.Ordinal))
            return string.Empty;

        return trimmed;
    }

    private static int? TryPositiveLine(int value)
    {
        if (value <= 0)
            return null;

        return value;
    }

    private static SchemaValidationService CreateSchemaValidationService()
    {
        SchemaValidationOptions options = new() { EnableDetailedErrors = true, EnableResultCaching = false };

        return new SchemaValidationService(NullLogger<SchemaValidationService>.Instance, Options.Create(options));
    }
}
