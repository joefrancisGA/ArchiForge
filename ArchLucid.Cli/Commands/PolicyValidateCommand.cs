using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     <c>archlucid policy validate &lt;file.json&gt;</c> — deserializes a <see cref="PolicyPackContentDocument" /> and
///     reports structural issues (no YAML).
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "Thin file I/O and JSON deserialization; exercised via CLI integration smoke if added.")]
internal static class PolicyValidateCommand
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions JsonOutCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task<int> RunAsync(string jsonPath)
    {
        string path = Path.GetFullPath(jsonPath.Trim());

        if (!File.Exists(path))
        {
            WriteErr(CliExitCode.OperationFailed, $"File not found: {path}");

            return Task.FromResult(CliExitCode.OperationFailed);
        }

        string raw;

        try

        {
            raw = File.ReadAllText(path);
        }
        catch (Exception ex)

        {
            WriteErr(CliExitCode.OperationFailed, $"Could not read file: {ex.Message}");

            return Task.FromResult(CliExitCode.OperationFailed);
        }

        string trimmed = raw.TrimStart();

        if (trimmed.Length == 0)
        {
            WriteErr(CliExitCode.UsageError, "File is empty.");

            return Task.FromResult(CliExitCode.UsageError);
        }

        if (trimmed[0] is not '{')
        {
            WriteErr(
                CliExitCode.UsageError,
                "Expected a JSON object (policy pack content uses JSON; use yaml-to-json tooling for YAML packs).");

            return Task.FromResult(CliExitCode.UsageError);
        }

        PolicyPackContentDocument? doc;

        try

        {
            doc = JsonSerializer.Deserialize<PolicyPackContentDocument>(raw, Json);
        }
        catch (JsonException jx)

        {
            WriteErr(CliExitCode.UsageError, $"Invalid JSON: {jx.Message}");

            return Task.FromResult(CliExitCode.UsageError);
        }

        if (doc is null)
        {
            WriteErr(CliExitCode.UsageError, "Deserialized document is null.");

            return Task.FromResult(CliExitCode.UsageError);
        }

        if (CliExecutionContext.JsonOutput)

        {
            Dictionary<string, object?> payload = new()
            {
                ["ok"] = true,
                ["complianceRuleIdCount"] = doc.ComplianceRuleIds.Count,
                ["complianceRuleKeyCount"] = doc.ComplianceRuleKeys.Count,
                ["alertRuleIdCount"] = doc.AlertRuleIds.Count,
                ["compositeAlertRuleIdCount"] = doc.CompositeAlertRuleIds.Count
            };

            Console.WriteLine(JsonSerializer.Serialize(payload, JsonOutCamel));
        }
        else

        {
            Console.WriteLine(
                $"Valid policy pack JSON: {path} " +
                $"(rules: {doc.ComplianceRuleIds.Count + doc.ComplianceRuleKeys.Count}, alerts: {doc.AlertRuleIds.Count}).");
        }

        return Task.FromResult(CliExitCode.Success);
    }

    private static void WriteErr(int exitCode, string message)
    {
        if (CliExecutionContext.JsonOutput)

            CliJson.WriteFailureLine(Console.Error, exitCode, "policy_validate", message);

        else

            Console.Error.WriteLine($"[policy validate] {message}");
    }
}
