using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Cli;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI comparisons subcommands orchestrate HTTP via ArchLucidApiClient (excluded from coverage); exercised via manual CLI and API integration.")]
internal static class ComparisonsCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine(
                "Usage: archlucid comparisons list [--type <type>] [--left-run <runId>] [--right-run <runId>] [--tag <tag>] [--skip <n>] [--limit <n>] [--cursor <cursor>] [--sort-by <createdUtc|type|label|leftRunId|rightRunId>] [--sort <asc|desc>] [--json|--table]");
            Console.WriteLine(
                "   or: archlucid comparisons replay <comparisonRecordId> [--format <markdown|html|docx|pdf>] [--mode <artifact|regenerate|verify>] [--profile <profile>] [--persist] [--out <path>] [--force]");
            Console.WriteLine(
                "   or: archlucid comparisons replay-batch <id1,id2,...> [--format ...] [--mode ...] [--profile ...] [--persist] [--out <path>] [--force]");
            Console.WriteLine("   or: archlucid comparisons summary <comparisonRecordId> [--json]");
            Console.WriteLine("   or: archlucid comparisons drift <comparisonRecordId> [--json]");
            Console.WriteLine("   or: archlucid comparisons diagnostics [--limit <n>] [--json|--table]");
            Console.WriteLine("   or: archlucid comparisons tag <comparisonRecordId> [--label <label>] [--tag <t>]...");

            return CliExitCode.UsageError;
        }

        string sub = args[0];
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);

        ApiConnectionOutcome connection = await CliCommandShared.TryConnectToApiAsync(baseUrl);

        if (connection != ApiConnectionOutcome.Connected)
        {
            return CliCommandShared.ExitCodeForFailedConnection(connection);
        }

        ArchLucidApiClient client = new(baseUrl);

        switch (sub)
        {
            case "list":
                return await ListAsync(client, args.Skip(1).ToArray());
            case "replay":
                return await ReplayAsync(client, args.Skip(1).ToArray());
            case "replay-batch":
                return await ReplayBatchAsync(client, args.Skip(1).ToArray());
            case "summary":
                return await SummaryAsync(client, args.Skip(1).ToArray());
            case "drift":
                return await DriftAsync(client, args.Skip(1).ToArray());
            case "diagnostics":
                return await DiagnosticsAsync(client, args.Skip(1).ToArray());
            case "tag":
                return await TagAsync(client, args.Skip(1).ToArray());
            default:
                Console.WriteLine($"Unknown subcommand for comparisons: {sub}");

                return CliExitCode.UsageError;
        }
    }

    private static async Task<int> ListAsync(ArchLucidApiClient client, string[] args)
    {
        string? type = null;
        string? leftRun = null;
        string? rightRun = null;
        string? leftExport = null;
        string? rightExport = null;
        string? label = null;
        string? tag = null;
        string? tags = null;
        string? cursor = null;
        string sortBy = "createdUtc";
        string sortDir = "desc";
        int skip = 0;
        int limit = 20;
        bool asJson = false;
        bool asTable = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--type" when i + 1 < args.Length:
                    type = args[++i];
                    break;
                case "--left-run" when i + 1 < args.Length:
                    leftRun = args[++i];
                    break;
                case "--right-run" when i + 1 < args.Length:
                    rightRun = args[++i];
                    break;
                case "--left-export" when i + 1 < args.Length:
                    leftExport = args[++i];
                    break;
                case "--right-export" when i + 1 < args.Length:
                    rightExport = args[++i];
                    break;
                case "--label" when i + 1 < args.Length:
                    label = args[++i];
                    break;
                case "--tag" when i + 1 < args.Length:
                    tag = args[++i];
                    break;
                case "--tags" when i + 1 < args.Length:
                    tags = args[++i];
                    break;
                case "--cursor" when i + 1 < args.Length:
                    cursor = args[++i];
                    break;
                case "--sort-by" when i + 1 < args.Length:
                    sortBy = args[++i];
                    break;
                case "--sort" when i + 1 < args.Length:
                    sortDir = args[++i];
                    break;
                case "--skip" when i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedSkip):
                    skip = parsedSkip;
                    i++;
                    break;
                case "--limit" when i + 1 < args.Length && int.TryParse(args[i + 1], out int parsed):
                    limit = parsed;
                    i++;
                    break;
                case "--json":
                    asJson = true;
                    break;
                case "--table":
                    asTable = true;
                    break;
            }
        }

        ArchLucidApiClient.ComparisonHistoryResult? result = await client.SearchComparisonsAsync(
            type,
            leftRun,
            rightRun,
            leftExport,
            rightExport,
            label,
            tag,
            tags,
            sortBy,
            sortDir,
            cursor,
            skip,
            limit);

        if (result is null)
        {
            Console.WriteLine("No comparison records found or request failed.");

            return CliExitCode.OperationFailed;
        }

        if (result.Records.Count == 0)
        {
            Console.WriteLine("No comparison records matched the filters.");

            return CliExitCode.Success;
        }

        if (asJson)
        {
            string json = JsonSerializer.Serialize(result, CliCommandShared.JsonWriteIndented);
            Console.WriteLine(json);

            if (string.IsNullOrWhiteSpace(result.NextCursor))
            {
                return CliExitCode.Success;
            }

            Console.WriteLine();
            Console.WriteLine($"nextCursor: {result.NextCursor}");

            return CliExitCode.Success;
        }

        if (asTable)
        {
            PrintComparisonTable(result.Records);

            return CliExitCode.Success;
        }

        foreach (ArchLucidApiClient.ComparisonRecordSummary r in result.Records)
        {
            string labelPart = string.IsNullOrEmpty(r.Label) ? "" : $" Label={r.Label}";
            string tagsPart = r.Tags.Count == 0 ? "" : " Tags=[" + string.Join(",", r.Tags) + "]";
            Console.WriteLine(
                $"{r.CreatedUtc:O} | {r.ComparisonRecordId} | {r.ComparisonType} | LeftRun={r.LeftRunId} RightRun={r.RightRunId}{labelPart}{tagsPart}");
        }

        return CliExitCode.Success;
    }

    private static void PrintComparisonTable(IReadOnlyList<ArchLucidApiClient.ComparisonRecordSummary> records)
    {
        List<string[]> rows = records.Select(r => new[]
            {
                r.CreatedUtc.ToString("O"),
                r.ComparisonRecordId,
                r.ComparisonType,
                r.LeftRunId ?? "",
                r.RightRunId ?? "",
                r.Label ?? "",
                r.Tags.Count == 0 ? "" : string.Join(",", r.Tags)
            })
            .ToList();

        string[] headers =
        [
            "CreatedUtc", "ComparisonRecordId", "Type", "LeftRunId", "RightRunId", "Label", "Tags"
        ];

        rows.Insert(0, headers);

        int[] widths = new int[headers.Length];

        for (int c = 0; c < headers.Length; c++)
        {
            widths[c] = rows.Max(r => r[c].Length);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            string line = string.Join(" | ", rows[i].Select((cell, idx) => cell.PadRight(widths[idx])));
            Console.WriteLine(line);

            if (i == 0)
            {
                Console.WriteLine(string.Join("-+-", widths.Select(w => new string('-', w))));
            }
        }
    }

    private static async Task<int> TagAsync(ArchLucidApiClient client, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: archlucid comparisons tag <comparisonRecordId> [--label <label>] [--tag <t>]...");

            return CliExitCode.UsageError;
        }

        string comparisonRecordId = args[0];
        string? label = null;
        List<string> tags = [];

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--label" when i + 1 < args.Length:
                    label = args[++i];
                    break;
                case "--tag" when i + 1 < args.Length:
                    tags.Add(args[++i]);
                    break;
            }
        }

        bool ok = await client.UpdateComparisonRecordAsync(comparisonRecordId, label, tags);

        if (!ok)
        {
            Console.WriteLine("Update failed or comparison record not found.");

            return CliExitCode.OperationFailed;
        }

        Console.WriteLine($"Updated comparison record {comparisonRecordId}.");

        return CliExitCode.Success;
    }

    private static async Task<int> ReplayAsync(ArchLucidApiClient client, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine(
                "Usage: archlucid comparisons replay <comparisonRecordId> [--format <markdown|html|docx|pdf>] [--mode <artifact|regenerate|verify>] [--profile <profile>] [--persist] [--out <path>] [--force]");

            return CliExitCode.UsageError;
        }

        string comparisonRecordId = args[0];
        string format = "markdown";
        string mode = "artifact";
        string? profile = null;
        bool persist = false;
        string? outPath = null;
        bool force = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format" when i + 1 < args.Length:
                    format = args[++i];
                    break;
                case "--mode" when i + 1 < args.Length:
                    mode = args[++i];
                    break;
                case "--profile" when i + 1 < args.Length:
                    profile = args[++i];
                    break;
                case "--persist":
                    persist = true;
                    break;
                case "--out" when i + 1 < args.Length:
                    outPath = args[++i];
                    break;
                case "--force":
                    force = true;
                    break;
            }
        }

        bool ok = await client.ReplayComparisonToFileAsync(comparisonRecordId, format, mode, profile, persist, outPath, force);

        return ok ? CliExitCode.Success : CliExitCode.OperationFailed;
    }

    private static async Task<int> ReplayBatchAsync(ArchLucidApiClient client, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine(
                "Usage: archlucid comparisons replay-batch <id1,id2,...> [--format <markdown|html|docx|pdf>] [--mode <artifact|regenerate|verify>] [--profile <profile>] [--persist] [--out <path>] [--force]");

            return CliExitCode.UsageError;
        }

        List<string> ids = args[0]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string format = "markdown";
        string mode = "artifact";
        string? profile = null;
        bool persist = false;
        string? outPath = null;
        bool force = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format" when i + 1 < args.Length:
                    format = args[++i];
                    break;
                case "--mode" when i + 1 < args.Length:
                    mode = args[++i];
                    break;
                case "--profile" when i + 1 < args.Length:
                    profile = args[++i];
                    break;
                case "--persist":
                    persist = true;
                    break;
                case "--out" when i + 1 < args.Length:
                    outPath = args[++i];
                    break;
                case "--force":
                    force = true;
                    break;
            }
        }

        bool ok = await client.ReplayComparisonsBatchToZipAsync(ids, format, mode, profile, persist, outPath, force);

        return ok ? CliExitCode.Success : CliExitCode.OperationFailed;
    }

    private static async Task<int> SummaryAsync(ArchLucidApiClient client, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: archlucid comparisons summary <comparisonRecordId> [--json]");

            return CliExitCode.UsageError;
        }

        string comparisonRecordId = args[0];
        bool asJson = args.Any(a => a == "--json");
        ArchLucidApiClient.ComparisonSummary? summary = await client.GetComparisonSummaryAsync(comparisonRecordId);

        if (summary is null)
        {
            Console.WriteLine("Failed to get comparison summary (unauthorized, not found, or request failed).");

            return CliExitCode.OperationFailed;
        }

        if (asJson)
        {
            string json = JsonSerializer.Serialize(summary, CliCommandShared.JsonWriteIndented);
            Console.WriteLine(json);

            return CliExitCode.Success;
        }

        Console.WriteLine(summary.Summary);

        return CliExitCode.Success;
    }

    private static async Task<int> DriftAsync(ArchLucidApiClient client, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: archlucid comparisons drift <comparisonRecordId> [--json]");

            return CliExitCode.UsageError;
        }

        string comparisonRecordId = args[0];
        bool asJson = args.Any(a => a == "--json");
        ArchLucidApiClient.DriftAnalysis? drift = await client.GetComparisonDriftAsync(comparisonRecordId);

        if (drift is null)
        {
            Console.WriteLine("Failed to get drift analysis (unauthorized, not found, or request failed).");

            return CliExitCode.OperationFailed;
        }

        if (asJson)
        {
            string json = JsonSerializer.Serialize(drift, CliCommandShared.JsonWriteIndented);
            Console.WriteLine(json);

            return CliExitCode.Success;
        }

        Console.WriteLine($"DriftDetected={drift.DriftDetected}");
        Console.WriteLine(drift.Summary);

        foreach (ArchLucidApiClient.DriftItem item in drift.Items.Take(25))
        {
            Console.WriteLine($"- [{item.Category}] {item.Path}: {item.Description}");
        }

        if (drift.Items.Count > 25)
        {
            Console.WriteLine($"(showing 25 of {drift.Items.Count} items)");
        }

        return CliExitCode.Success;
    }

    private static async Task<int> DiagnosticsAsync(ArchLucidApiClient client, string[] args)
    {
        int limit = 20;
        bool asJson = false;
        bool asTable = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[i + 1], out int parsed))
            {
                limit = parsed;
                i++;
            }

            if (args[i] == "--json")
            {
                asJson = true;
            }

            if (args[i] == "--table")
            {
                asTable = true;
            }
        }

        ArchLucidApiClient.ReplayDiagnostics? diagnostics = await client.GetReplayDiagnosticsAsync(limit);

        if (diagnostics is null)
        {
            Console.WriteLine("Failed to get replay diagnostics (unauthorized or request failed).");

            return CliExitCode.OperationFailed;
        }

        if (asJson)
        {
            string json = JsonSerializer.Serialize(diagnostics, CliCommandShared.JsonWriteIndented);
            Console.WriteLine(json);

            return CliExitCode.Success;
        }

        if (asTable)
        {
            PrintReplayDiagnosticsTable(diagnostics.RecentReplays);

            return CliExitCode.Success;
        }

        foreach (ArchLucidApiClient.ReplayDiagnosticsEntry e in diagnostics.RecentReplays)
        {
            Console.WriteLine(
                $"{e.TimestampUtc:O} | {e.ComparisonRecordId} | {e.ComparisonType} | {e.Format} | {e.ReplayMode} | Success={e.Success} | {e.DurationMs}ms | MetaOnly={e.MetadataOnly} | Persisted={e.PersistedReplayRecordId} | Err={e.ErrorMessage}");
        }

        return CliExitCode.Success;
    }

    private static void PrintReplayDiagnosticsTable(IReadOnlyList<ArchLucidApiClient.ReplayDiagnosticsEntry> entries)
    {
        List<string[]> rows = entries.Select(e => new[]
            {
                e.TimestampUtc.ToString("O"),
                e.ComparisonRecordId,
                e.ComparisonType,
                e.Format,
                e.ReplayMode,
                e.Success ? "true" : "false",
                e.DurationMs.ToString(),
                e.MetadataOnly ? "true" : "false",
                e.PersistedReplayRecordId ?? "",
                e.ErrorMessage ?? ""
            })
            .ToList();

        string[] headers =
        [
            "TimestampUtc",
            "ComparisonRecordId",
            "Type",
            "Format",
            "Mode",
            "Success",
            "Ms",
            "MetaOnly",
            "PersistedReplayRecordId",
            "Error"
        ];

        rows.Insert(0, headers);

        int[] widths = new int[headers.Length];

        for (int c = 0; c < headers.Length; c++)
        {
            widths[c] = rows.Max(r => r[c].Length);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            string line = string.Join(" | ", rows[i].Select((cell, idx) => cell.PadRight(widths[idx])));
            Console.WriteLine(line);

            if (i == 0)
            {
                Console.WriteLine(string.Join("-+-", widths.Select(w => new string('-', w))));
            }
        }
    }
}
