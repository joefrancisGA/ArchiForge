using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Agents;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Offline rollup of persisted <see cref="AgentOutputEvaluationSummary" /> JSON (for example exported from
///     <c>GET /v1/architecture/run/{runId}/agent-evaluation</c>) — does not imply real-LLM vs simulator mode labels.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Console + file IO; exercised via Cli tests.")]
internal static class AgentEvalRollupCommand
{
    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    static AgentEvalRollupCommand()
    {
        JsonRead.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        string? jsonPath = null;
        bool jsonOut = args.Contains("--json", StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            if (string.Equals(a, "--from-json", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                jsonPath = args[i + 1];
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            await Console.Error.WriteLineAsync("Usage: archlucid agent-eval rollup --from-json <agent-evaluation.json> [--json]");
            await Console.Error.WriteLineAsync(
                "Input JSON must deserialize to / match the AgentOutputEvaluationSummary contract (camelCase-friendly). "
                + "Real-LLM vs simulator posture is NOT embedded in this export — correlate with deployment / run footer.");

            return CliExitCode.UsageError;
        }

        if (!File.Exists(jsonPath))
        {
            await Console.Error.WriteLineAsync($"Not found: {jsonPath}");

            return CliExitCode.UsageError;
        }

        await using FileStream stream = File.OpenRead(jsonPath);

        AgentOutputEvaluationSummary? summary =
            await JsonSerializer.DeserializeAsync<AgentOutputEvaluationSummary>(stream, JsonRead, cancellationToken)
                .ConfigureAwait(false);

        if (summary is null)
        {
            await Console.Error.WriteLineAsync("Deserialization returned null.");

            return CliExitCode.UsageError;
        }

        if (jsonOut)
        {
            Console.WriteLine(
                JsonSerializer.Serialize(
                    BuildRollupModel(summary),
                    new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

            return CliExitCode.Success;
        }

        Console.Write(FormatMarkdown(summary));

        return CliExitCode.Success;
    }

    private static AgentEvalRollupModel BuildRollupModel(AgentOutputEvaluationSummary summary)
    {
        int parseFails = summary.Scores.Count(static s => s.IsJsonParseFailure);
        Dictionary<string, int> byAgent = summary.Scores
            .GroupBy(static s => s.AgentType.ToString())
            .ToDictionary(static g => g.Key, static g => g.Count(), StringComparer.Ordinal);

        Dictionary<string, double> semanticMeans = summary.Scores
            .Where(static s => s.Semantic is not null)
            .GroupBy(static s => s.AgentType.ToString())
            .ToDictionary(
                static g => g.Key,
                static g => g.Average(x => x.Semantic!.OverallSemanticScore),
                StringComparer.Ordinal);

        Dictionary<string, double> structuralMeans = summary.Scores
            .Where(static s => !s.IsJsonParseFailure)
            .GroupBy(static s => s.AgentType.ToString())
            .ToDictionary(
                static g => g.Key,
                static g => g.Average(x => x.StructuralCompletenessRatio),
                StringComparer.Ordinal);

        return new AgentEvalRollupModel(
            summary.RunId,
            summary.EvaluatedAtUtc,
            summary.TracesSkippedCount,
            summary.Scores.Count,
            parseFails,
            summary.AverageStructuralCompletenessRatio,
            summary.AverageSemanticScore,
            byAgent,
            structuralMeans,
            semanticMeans);
    }

    private static string FormatMarkdown(AgentOutputEvaluationSummary summary)
    {
        AgentEvalRollupModel rollup = BuildRollupModel(summary);
        StringBuilder sb = new();

        sb.AppendLine("# Agent output evaluation rollup (offline JSON)");
        sb.AppendLine();

        sb.AppendLine(
            "**Execution mode caveat:** simulator vs real AOAI classification is **not** present in agent-evaluation exports — "
                + "read `architecture/run` payloads / provenance footer for host posture.");

        sb.AppendLine();
        sb.AppendLine(FormattableString.Invariant($"- Run: `{rollup.RunId}`"));
        sb.AppendLine(FormattableString.Invariant($"- EvaluatedAtUtc (payload): `{rollup.EvaluatedAtUtc:O}`"));
        sb.AppendLine(FormattableString.Invariant(
            $"- Scored traces: `{rollup.ScoresCounted}` · skipped traces: `{rollup.TracesSkippedCount}` · parse failures inside scores: `{rollup.ParseFailuresInsideScores}`"));

        string structuralLine = rollup.PayloadStructuralMean is double sm
            ? FormattableString.Invariant($"- Mean structural completeness (payload averages): {sm:P1}")
            : "- Mean structural completeness (payload averages): —";

        sb.AppendLine(structuralLine);

        string semanticLine = rollup.PayloadSemanticMean is double se
            ? FormattableString.Invariant($"- Mean semantic score (payload averages): {se:P1}")
            : "- Mean semantic score (payload averages): —";

        sb.AppendLine(semanticLine);
        sb.AppendLine();

        if (rollup.CountByAgentType.Count > 0)
        {
            sb.AppendLine("## Rows by agent role");
            sb.AppendLine();

            foreach ((string agent, int ct) in rollup.CountByAgentType.OrderByDescending(static kv => kv.Value))
                sb.AppendLine(FormattableString.Invariant($"- `{agent}`: {ct}"));

            sb.AppendLine();
        }

        if (rollup.StructuralMeanByAgentType.Count > 0)
        {
            sb.AppendLine("## Mean structural completeness by agent role (non-parse-failure rows)");
            sb.AppendLine();

            foreach ((string agent, double mean) in rollup.StructuralMeanByAgentType.OrderBy(static kv => kv.Key))
                sb.AppendLine(FormattableString.Invariant($"- `{agent}`: {mean:P1}"));

            sb.AppendLine();
        }

        if (rollup.SemanticMeanByAgentType.Count > 0)
        {
            sb.AppendLine("## Mean semantic score by agent role (where semantic scorer ran)");
            sb.AppendLine();

            foreach ((string agent, double mean) in rollup.SemanticMeanByAgentType.OrderBy(static kv => kv.Key))
                sb.AppendLine(FormattableString.Invariant($"- `{agent}`: {mean:P1}"));

            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("See `docs/library/AGENT_OUTPUT_EVALUATION.md` (Rollup CLI) for instrumentation cross-links.");

        return sb.ToString();
    }

    private sealed record AgentEvalRollupModel(
        string RunId,
        DateTime EvaluatedAtUtc,
        int TracesSkippedCount,
        int ScoresCounted,
        int ParseFailuresInsideScores,
        double? PayloadStructuralMean,
        double? PayloadSemanticMean,
        Dictionary<string, int> CountByAgentType,
        Dictionary<string, double> StructuralMeanByAgentType,
        Dictionary<string, double> SemanticMeanByAgentType);
}
