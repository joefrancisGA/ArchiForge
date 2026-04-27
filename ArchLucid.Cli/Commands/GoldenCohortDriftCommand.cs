using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.GoldenCorpus;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     <c>archlucid golden-cohort drift</c> — compare live cohort output (SHA + optional finding categories) with
///     <c>cohort.json</c> expectations, with optional real-LLM JSON structural checks.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "HTTP orchestration; core logic in ArchLucid.Core.GoldenCorpus and tests.")]
internal static class GoldenCohortDriftCommand
{
    private static readonly JsonSerializerOptions StdoutJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<int> RunAsync(string[] args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        bool strictReal = false;
        bool structuralOnly = false;
        string? cohortPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            string token = args[i];

            if (string.Equals(token, "--strict-real", StringComparison.Ordinal))
            {
                strictReal = true;

                continue;
            }

            if (string.Equals(token, "--structural-only", StringComparison.Ordinal))
            {
                structuralOnly = true;

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

        if (strictReal && !IsRealLlmContext())
        {
            await Console.Error.WriteLineAsync(
                "Refusing --strict-real: set ARCHLUCID_GOLDEN_COHORT_REAL_LLM=true and/or " +
                "ARCHLUCID_AGENT_EXECUTION_MODE/AgentExecution__Mode=Real so the gate targets a real-LLM API host.");

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
        int cap = document.Items.Count;
        string? capRaw = Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_DRIFT_ITEM_CAP");

        if (int.TryParse(capRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedCap) && parsedCap > 0
            && parsedCap < cap)
            cap = parsedCap;

        bool runStructural = strictReal || structuralOnly;
        List<GoldenCohortDriftStructuralFailure> structuralFailures = [];

        for (int index = 0; index < cap; index++)
        {
            GoldenCohortItem item = document.Items[index];

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                await Console.Error.WriteLineAsync(
                    $"Cohort item at index {index.ToString(CultureInfo.InvariantCulture)} has an empty id.");

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

            if (!structuralOnly)
            {
                ArchLucidApiClient.GoldenManifestFingerprintResult? fingerprint =
                    await client.TryCommitAndFingerprintGoldenManifestAsync(runId);

                if (fingerprint is null || !fingerprint.Success || string.IsNullOrWhiteSpace(fingerprint.Sha256HexUpper))
                {
                    await Console.Error.WriteLineAsync(
                        $"[{item.Id}] commit/fingerprint failed: {fingerprint?.Error ?? "unknown"}");

                    return CliExitCode.OperationFailed;
                }

                string actualShaLower = fingerprint.Sha256HexUpper.ToLowerInvariant();
                string expectedSha = item.ExpectedCommittedManifestSha256.Trim();

                if (!string.Equals(actualShaLower, expectedSha, StringComparison.OrdinalIgnoreCase))
                {
                    await Console.Error.WriteLineAsync(
                        $"[{item.Id}] committed manifest SHA mismatch. expected={expectedSha} actual={actualShaLower}");

                    return CliExitCode.OperationFailed;
                }
            }

            ArchLucidApiClient.GetRunResult? getRun = await client.GetRunAsync(runId);

            if (getRun is null)
            {
                await Console.Error.WriteLineAsync($"[{item.Id}] get run failed.");

                return CliExitCode.OperationFailed;
            }

            if (strictReal)
            {
                if (getRun.Run.RealModeFellBackToSimulator is true)
                {
                    GoldenCohortDriftStructuralFailure fb = new()
                    {
                        CohortItemId = item.Id,
                        RunId = runId,
                        Code = "realModeFellBackToSimulator",
                        Message =
                            "Run recorded RealModeFellBackToSimulator=true; strict-real cannot validate real-LLM JSON shape."
                    };
                    structuralFailures.Add(fb);
                    await Console.Out.WriteLineAsync(
                        JsonSerializer.Serialize(new
                        {
                            success = false,
                            failure = fb
                        }, StdoutJson));

                    return CliExitCode.OperationFailed;
                }
            }

            List<AgentResult>? agentResults = TryParseAgentResults(getRun.Results, item.Id, out string? parseError);

            if (parseError is not null)
            {
                await Console.Error.WriteLineAsync(parseError);

                return CliExitCode.OperationFailed;
            }

            if (agentResults is null)
                return CliExitCode.OperationFailed;

            if (!structuralOnly)
            {
                SortedSet<string> actualCategories = GoldenCohortFindingCategoryAggregator.DistinctCategories(agentResults);
                SortedSet<string> expectedCategories = new(StringComparer.Ordinal);

                foreach (string c in item.ExpectedFindingCategories.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    expectedCategories.Add(c.Trim());
                }

                if (!actualCategories.SetEquals(expectedCategories))
                {
                    await Console.Error.WriteLineAsync(
                        $"[{item.Id}] finding category multiset mismatch. expected={string.Join(", ", expectedCategories)} " +
                        $"actual={string.Join(", ", actualCategories)}");

                    return CliExitCode.OperationFailed;
                }
            }

            if (!runStructural)
                continue;

            // Serialize the raw API result object so extra JSON (e.g. per-finding `trace`) is not dropped by
            // ArchLucid.Contracts.Agents.AgentResult round-trip.
            for (int ri = 0; ri < getRun.Results.Count; ri++)
            {
                object raw = getRun.Results[ri];
                string resultJson = JsonSerializer.Serialize(raw, ContractJson.Default);
                AgentResult? r = JsonSerializer.Deserialize<AgentResult>(resultJson, ContractJson.Default);

                if (r is null)
                {
                    await Console.Error.WriteLineAsync(
                        $"[{item.Id}] could not read agentType for structural validation at result index {ri.ToString(CultureInfo.InvariantCulture)}.");

                    return CliExitCode.OperationFailed;
                }

                RealLlmStructuralValidationResult v =
                    RealLlmOutputStructuralValidator.ValidateAgentResultStructure(r.AgentType.ToString(), resultJson);

                if (v.IsValid)
                    continue;

                structuralFailures.Add(
                    new GoldenCohortDriftStructuralFailure
                    {
                        CohortItemId = item.Id,
                        RunId = runId,
                        Code = "structuralValidation",
                        Message = "One or more structural checks failed for an agent result.",
                        AgentType = r.AgentType.ToString(),
                        ResultId = r.ResultId,
                        Validation = v
                    });
            }
        }

        if (structuralFailures.Count > 0)
        {
            object report = new
            {
                success = false,
                kind = "goldenCohortDrift",
                strictReal,
                structuralOnly,
                structuralFailures
            };
            await Console.Out.WriteLineAsync(JsonSerializer.Serialize(report, StdoutJson));

            return CliExitCode.OperationFailed;
        }

        if (!CliExecutionContext.JsonOutput)
        {
            Console.WriteLine(
                structuralOnly
                    ? "OK — golden-cohort structural validation passed (SHA comparison skipped)."
                    : strictReal
                        ? "OK — golden-cohort drift passed (SHA, categories, structural validation)."
                        : "OK — golden-cohort drift passed (SHA, categories).");
        }
        else
        {
            object ok = new
            {
                success = true,
                kind = "goldenCohortDrift",
                strictReal,
                structuralOnly,
                cohortPath = resolvedCohort
            };
            Console.WriteLine(JsonSerializer.Serialize(ok, JsonCamel));
        }

        return CliExitCode.Success;
    }

    private static List<AgentResult>? TryParseAgentResults(
        List<object> raw,
        string itemId,
        out string? error)
    {
        error = null;

        List<AgentResult> list = [];
        int i = 0;

        foreach (AgentResult? ar in raw.Select(o => JsonSerializer.Serialize(o, ContractJson.Default)).Select(j => JsonSerializer.Deserialize<AgentResult>(j, ContractJson.Default)))
        {
            if (ar is null)
            {
                error = $"[{itemId}] could not deserialize agent result at index {i.ToString(CultureInfo.InvariantCulture)}.";

                return null;
            }

            list.Add(ar);
            i++;
        }

        if (list.Count != 0)
            return list;
        error = $"[{itemId}] no agent results returned for drift analysis.";

        return null;
    }

    private static bool IsRealLlmContext() =>
        IsTruthyEnvironment("ARCHLUCID_GOLDEN_COHORT_REAL_LLM")
        || string.Equals(
            Environment.GetEnvironmentVariable("ARCHLUCID_AGENT_EXECUTION_MODE")?.Trim() ?? string.Empty,
            "Real",
            StringComparison.OrdinalIgnoreCase)
        || string.Equals(
            Environment.GetEnvironmentVariable("AgentExecution__Mode")?.Trim() ?? string.Empty,
            "Real",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsTruthyEnvironment(string name)
    {
        string? raw = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string v = raw.Trim();

        return string.Equals(v, "1", StringComparison.Ordinal)
               || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class GoldenCohortDriftStructuralFailure
    {
        public string? Code
        {
            get;
            set;
        }

        public string? Message
        {
            get;
            set;
        }

        public string? CohortItemId
        {
            get;
            set;
        }

        public string? RunId
        {
            get;
            set;
        }

        public string? AgentType
        {
            get;
            set;
        }

        public string? ResultId
        {
            get;
            set;
        }

        public RealLlmStructuralValidationResult? Validation
        {
            get;
            set;
        }
    }
}
