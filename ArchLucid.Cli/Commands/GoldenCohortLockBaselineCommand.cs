using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Application.GoldenCohort;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Cli.Commands;

/// <summary><c>archlucid golden-cohort lock-baseline</c> — simulator-only manifest SHA capture for <c>tests/golden-cohort/cohort.json</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "Console + HTTP orchestration; guards covered by unit tests.")]
internal static class GoldenCohortLockBaselineCommand
{
    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<int> RunAsync(string[] args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        if (IsTruthyEnvironmentFlag("ARCHLUCID_GOLDEN_COHORT_REAL_LLM"))
        {
            await Console.Error.WriteLineAsync(
                "Refusing golden-cohort lock-baseline: ARCHLUCID_GOLDEN_COHORT_REAL_LLM is enabled (real-LLM cohort — budget item 15).");

            return CliExitCode.UsageError;
        }

        string? agentMode =
            Environment.GetEnvironmentVariable("ARCHLUCID_AGENT_EXECUTION_MODE")
            ?? Environment.GetEnvironmentVariable("AgentExecution__Mode");

        if (string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase))
        {
            await Console.Error.WriteLineAsync(
                "Refusing golden-cohort lock-baseline: agent execution mode is Real in this shell. " +
                "Use a simulator-configured API host (AgentExecution:Mode=Simulator) or unset ARCHLUCID_AGENT_EXECUTION_MODE / AgentExecution__Mode.");

            return CliExitCode.UsageError;
        }

        bool write = false;
        string? cohortPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            string token = args[i];

            if (string.Equals(token, "--write", StringComparison.Ordinal))
            {
                write = true;

                continue;
            }

            if (string.Equals(token, "--cohort", StringComparison.Ordinal))
            {
                if (i + 1 >= args.Length)
                {
                    await Console.Error.WriteLineAsync("Missing value for --cohort.");

                    return CliExitCode.UsageError;
                }

                cohortPath = args[++i].Trim();

                continue;
            }

            await Console.Error.WriteLineAsync($"Unexpected argument: {token}");

            return CliExitCode.UsageError;
        }

        if (write && !IsTruthyEnvironmentFlag("ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED"))
        {
            await Console.Error.WriteLineAsync(
                "Refusing --write: owner approval is required (docs/PENDING_QUESTIONS.md item 33). " +
                "Set ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED=true for this shell only after explicit approval.");

            return CliExitCode.UsageError;
        }

        string? repoRoot = CliRepositoryRootResolver.TryResolveRepositoryRoot();
        string resolvedCohort = string.IsNullOrWhiteSpace(cohortPath)
            ? Path.Combine(repoRoot ?? Directory.GetCurrentDirectory(), "tests", "golden-cohort", "cohort.json")
            : Path.GetFullPath(cohortPath);

        if (!File.Exists(resolvedCohort))
        {
            await Console.Error.WriteLineAsync($"Cohort file not found: {resolvedCohort}");

            return CliExitCode.UsageError;
        }

        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);
        ApiConnectionOutcome connection = await CliCommandShared.TryConnectToApiAsync(baseUrl, config);

        if (connection != ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(connection);

        ArchLucidApiClient client = new(baseUrl, config);
        GoldenCohortDocument document = GoldenCohortDocument.Load(resolvedCohort);
        List<object> jsonRows = [];

        for (int index = 0; index < document.Items.Count; index++)
        {
            GoldenCohortItem item = document.Items[index];

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                await Console.Error.WriteLineAsync($"Cohort item at index {index.ToString(CultureInfo.InvariantCulture)} has an empty id.");

                return CliExitCode.OperationFailed;
            }

            ArchitectureRequest request = GoldenCohortArchitectureRequestFactory.FromCohortItem(item);
            ArchLucidApiClient.CreateRunResult created = await client.CreateRunAsync(request);

            if (!created.Success || created.Response is null)
            {
                await Console.Error.WriteLineAsync($"[{item.Id}] create failed: {created.Error}");

                return CliExitCode.OperationFailed;
            }

            string runId = created.Response.Run.RunId;

            ArchLucidApiClient.ExecuteRunResult? executed = await client.ExecuteRunAsync(runId);

            if (executed is null || !executed.Success)
            {
                await Console.Error.WriteLineAsync($"[{item.Id}] execute failed: {executed?.Error ?? "unknown"}");

                return CliExitCode.OperationFailed;
            }

            ArchLucidApiClient.GoldenManifestFingerprintResult? fingerprint = await client.TryCommitAndFingerprintGoldenManifestAsync(runId);

            if (fingerprint is null || !fingerprint.Success || string.IsNullOrWhiteSpace(fingerprint.Sha256HexUpper))
            {
                await Console.Error.WriteLineAsync($"[{item.Id}] commit/fingerprint failed: {fingerprint?.Error ?? "unknown"}");

                return CliExitCode.OperationFailed;
            }

            string shaLower = fingerprint.Sha256HexUpper.ToLowerInvariant();

            if (write)
                item.ExpectedCommittedManifestSha256 = shaLower;

            if (CliExecutionContext.JsonOutput)
                jsonRows.Add(new { id = item.Id, committedManifestSha256 = shaLower });
            else
                Console.WriteLine($"{item.Id}\t{shaLower}");
        }

        if (write)
        {
            document.Save(resolvedCohort);

            if (!CliExecutionContext.JsonOutput)
                Console.WriteLine($"Wrote updated SHAs to {resolvedCohort}");
        }

        if (CliExecutionContext.JsonOutput)
        {
            object payload = new
            {
                cohortPath = resolvedCohort,
                wrote = write,
                items = jsonRows,
            };

            Console.WriteLine(JsonSerializer.Serialize(payload, JsonCamel));
        }

        return CliExitCode.Success;
    }

    private static bool IsTruthyEnvironmentFlag(string name)
    {
        string? raw = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string v = raw.Trim();

        return string.Equals(v, "1", StringComparison.Ordinal)
            || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
