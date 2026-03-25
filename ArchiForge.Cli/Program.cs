using System.Diagnostics;
using System.Text.Json;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Cli
{
    public static class Program
    {
        private static readonly JsonSerializerOptions SJsonWriteIndented = new() { WriteIndented = true };

        private static readonly JsonSerializerOptions SJsonDeserializeAgentResult = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static async Task<int> Main(string[] args) => await RunAsync(args);

        /// <summary>
        /// Entry point for the CLI. Used by tests to assert exit codes and behavior.
        /// </summary>
        public static async Task<int> RunAsync(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a command. Available commands: new, dev up, run [--quick], status <runId>, submit <runId> <result.json>, commit <runId>, seed <runId>, artifacts <runId>, comparisons list [filters], comparisons replay <comparisonRecordId> [--format <f>] [--mode <m>] [--profile <p>] [--persist], health");
                return 1;
            }

            string command = args[0];

            switch (command)
            {
                case "new":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archiforge new <projectName>");
                        return 1;
                    }
                    ArchiForge_New(args[1]);
                    return 0;

                case "dev":
                    if (args.Length > 1 && args[1] == "up")
                        return ArchiForge_Dev_Up();
                    Console.WriteLine("Expected: archiforge dev up");
                    return 1;

                case "run":
                    bool quick = args.Length > 1 && args[1] == "--quick";
                    return await ArchiForge_RunAsync(quick);

                case "status":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archiforge status <runId>");
                        return 1;
                    }
                    return await ArchiForge_StatusAsync(args[1]);

                case "submit":
                    if (args.Length <= 2)
                    {
                        Console.WriteLine("Usage: archiforge submit <runId> <result.json>");
                        return 1;
                    }
                    return await ArchiForge_SubmitAsync(args[1], args[2]);

                case "commit":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archiforge commit <runId>");
                        return 1;
                    }
                    return await ArchiForge_CommitAsync(args[1]);

                case "seed":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archiforge seed <runId>");
                        return 1;
                    }
                    return await ArchiForge_SeedAsync(args[1]);

                case "artifacts":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archiforge artifacts <runId> [--save]");
                        return 1;
                    }
                    bool saveArtifacts = args.Length > 2 && args[2] == "--save";
                    return await ArchiForge_ArtifactsAsync(args[1], saveArtifacts);

                case "comparisons":
                    return await ArchiForge_ComparisonsAsync(args.Skip(1).ToArray());

                case "health":
                    return await ArchiForge_HealthAsync();

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    return 1;
            }
        }


        private static void ArchiForge_New(string projectName)
        {
            Console.WriteLine("Creating ArchiForge project " + projectName);
            ArchiForgeProjectScaffolder.ScaffoldOptions scaffoldOptions = new ArchiForgeProjectScaffolder.ScaffoldOptions
            {
                ProjectName = projectName,
                BaseDirectory = null,
                OverwriteExistingFiles = true,
                IncludeTerraformStubs = true
            };
            ArchiForgeProjectScaffolder.CreateProject(scaffoldOptions);
        }

        private static ArchiForgeProjectScaffolder.ArchiForgeConfig? TryLoadConfigFromCwd()
        {
            try
            {
                return ArchiForgeProjectScaffolder.LoadConfig(Directory.GetCurrentDirectory());
            }
            catch
            {
                return null;
            }
        }

        private static string GetBaseUrl(ArchiForgeProjectScaffolder.ArchiForgeConfig? config) =>
            ArchiForgeApiClient.ResolveBaseUrl(config);

        /// <summary>
        /// Ensures the ArchiForge API is reachable. Returns true if connected, false otherwise (and prints an error).
        /// </summary>
        private static async Task<bool> EnsureApiConnectedAsync(string baseUrl, CancellationToken ct = default)
        {
            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);
            if (await client.CheckHealthAsync(ct))
                return true;
            Console.WriteLine($"Cannot connect to ArchiForge API at {baseUrl}");
            Console.WriteLine("Ensure the API is running: dotnet run --project ArchiForge.Api");
            Console.WriteLine("Or set apiUrl in archiforge.json / ARCHIFORGE_API_URL environment variable.");
            return false;
        }

        private static async Task<int> ArchiForge_HealthAsync()
        {
            string baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);
            if (await client.CheckHealthAsync())
            {
                Console.WriteLine($"OK - ArchiForge API at {baseUrl} is reachable.");
                return 0;
            }
            Console.WriteLine($"FAIL - Cannot reach ArchiForge API at {baseUrl}");
            Console.WriteLine("Ensure the API is running: dotnet run --project ArchiForge.Api");
            return 1;
        }

        private static async Task<int> ArchiForge_ComparisonsAsync(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: archiforge comparisons list [--type <type>] [--left-run <runId>] [--right-run <runId>] [--tag <tag>] [--skip <n>] [--limit <n>] [--cursor <cursor>] [--sort-by <createdUtc|type|label|leftRunId|rightRunId>] [--sort <asc|desc>] [--json|--table]");
                Console.WriteLine("   or: archiforge comparisons replay <comparisonRecordId> [--format <markdown|html|docx|pdf>] [--mode <artifact|regenerate|verify>] [--profile <profile>] [--persist] [--out <path>] [--force]");
                Console.WriteLine("   or: archiforge comparisons replay-batch <id1,id2,...> [--format ...] [--mode ...] [--profile ...] [--persist] [--out <path>] [--force]");
                Console.WriteLine("   or: archiforge comparisons summary <comparisonRecordId> [--json]");
                Console.WriteLine("   or: archiforge comparisons drift <comparisonRecordId> [--json]");
                Console.WriteLine("   or: archiforge comparisons diagnostics [--limit <n>] [--json|--table]");
                Console.WriteLine("   or: archiforge comparisons tag <comparisonRecordId> [--label <label>] [--tag <t>]...");
                return 1;
            }

            string sub = args[0];
            ArchiForgeProjectScaffolder.ArchiForgeConfig? config = TryLoadConfigFromCwd();
            string baseUrl = GetBaseUrl(config);
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);

            switch (sub)
            {
                case "list":
                    return await ArchiForge_Comparisons_ListAsync(client, args.Skip(1).ToArray());
                case "replay":
                    return await ArchiForge_Comparisons_ReplayAsync(client, args.Skip(1).ToArray());
                case "replay-batch":
                    return await ArchiForge_Comparisons_ReplayBatchAsync(client, args.Skip(1).ToArray());
                case "summary":
                    return await ArchiForge_Comparisons_SummaryAsync(client, args.Skip(1).ToArray());
                case "drift":
                    return await ArchiForge_Comparisons_DriftAsync(client, args.Skip(1).ToArray());
                case "diagnostics":
                    return await ArchiForge_Comparisons_DiagnosticsAsync(client, args.Skip(1).ToArray());
                case "tag":
                    return await ArchiForge_Comparisons_TagAsync(client, args.Skip(1).ToArray());
                default:
                    Console.WriteLine($"Unknown subcommand for comparisons: {sub}");
                    return 1;
            }
        }

        private static async Task<int> ArchiForge_Comparisons_ListAsync(ArchiForgeApiClient client, string[] args)
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

            ArchiForgeApiClient.ComparisonHistoryResult? result = await client.SearchComparisonsAsync(
                type, leftRun, rightRun,
                leftExport, rightExport,
                label,
                tag, tags,
                sortBy,
                sortDir,
                cursor,
                skip, limit);
            if (result is null)
            {
                Console.WriteLine("No comparison records found or request failed.");
                return 1;
            }

            if (result.Records.Count == 0)
            {
                Console.WriteLine("No comparison records matched the filters.");
                return 0;
            }

            if (asJson)
            {
                string json = JsonSerializer.Serialize(result, SJsonWriteIndented);
                Console.WriteLine(json);
                if (!string.IsNullOrWhiteSpace(result.NextCursor))
                {
                    // Keep it easy to copy/paste into the next call.
                    Console.WriteLine();
                    Console.WriteLine($"nextCursor: {result.NextCursor}");
                }
                return 0;
            }

            if (asTable)
            {
                PrintComparisonTable(result.Records);
                return 0;
            }

            foreach (ArchiForgeApiClient.ComparisonRecordSummary r in result.Records)
            {
                string labelPart = string.IsNullOrEmpty(r.Label) ? "" : $" Label={r.Label}";
                string tagsPart = r.Tags.Count == 0 ? "" : " Tags=[" + string.Join(",", r.Tags) + "]";
                Console.WriteLine($"{r.CreatedUtc:O} | {r.ComparisonRecordId} | {r.ComparisonType} | LeftRun={r.LeftRunId} RightRun={r.RightRunId}{labelPart}{tagsPart}");
            }

            return 0;
        }

        private static void PrintComparisonTable(IReadOnlyList<ArchiForgeApiClient.ComparisonRecordSummary> records)
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
            }).ToList();

            string[] headers = new[] { "CreatedUtc", "ComparisonRecordId", "Type", "LeftRunId", "RightRunId", "Label", "Tags" };
            rows.Insert(0, headers);

            int[] widths = new int[headers.Length];
            for (int c = 0; c < headers.Length; c++)
                widths[c] = rows.Max(r => r[c].Length);

            for (int i = 0; i < rows.Count; i++)
            {
                string line = string.Join(" | ", rows[i].Select((cell, idx) => cell.PadRight(widths[idx])));
                Console.WriteLine(line);
                if (i == 0)
                    Console.WriteLine(string.Join("-+-", widths.Select(w => new string('-', w))));
            }
        }

        private static async Task<int> ArchiForge_Comparisons_TagAsync(ArchiForgeApiClient client, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: archiforge comparisons tag <comparisonRecordId> [--label <label>] [--tag <t>]...");
                return 1;
            }

            string comparisonRecordId = args[0];
            string? label = null;
            List<string> tags = new List<string>();

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
                return 1;
            }
            Console.WriteLine($"Updated comparison record {comparisonRecordId}.");
            return 0;
        }

        private static async Task<int> ArchiForge_Comparisons_ReplayAsync(ArchiForgeApiClient client, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: archiforge comparisons replay <comparisonRecordId> [--format <markdown|html|docx|pdf>] [--mode <artifact|regenerate|verify>] [--profile <profile>] [--persist] [--out <path>] [--force]");
                return 1;
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
            return ok ? 0 : 1;
        }

        private static async Task<int> ArchiForge_Comparisons_ReplayBatchAsync(ArchiForgeApiClient client, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: archiforge comparisons replay-batch <id1,id2,...> [--format <markdown|html|docx|pdf>] [--mode <artifact|regenerate|verify>] [--profile <profile>] [--persist] [--out <path>] [--force]");
                return 1;
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
            return ok ? 0 : 1;
        }

        private static async Task<int> ArchiForge_Comparisons_SummaryAsync(ArchiForgeApiClient client, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: archiforge comparisons summary <comparisonRecordId> [--json]");
                return 1;
            }

            string comparisonRecordId = args[0];
            bool asJson = args.Any(a => a == "--json");
            ArchiForgeApiClient.ComparisonSummary? summary = await client.GetComparisonSummaryAsync(comparisonRecordId);
            if (summary is null)
            {
                Console.WriteLine("Failed to get comparison summary (unauthorized, not found, or request failed).");
                return 1;
            }

            if (asJson)
            {
                string json = JsonSerializer.Serialize(summary, SJsonWriteIndented);
                Console.WriteLine(json);
                return 0;
            }

            Console.WriteLine(summary.Summary);
            return 0;
        }

        private static async Task<int> ArchiForge_Comparisons_DriftAsync(ArchiForgeApiClient client, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: archiforge comparisons drift <comparisonRecordId> [--json]");
                return 1;
            }

            string comparisonRecordId = args[0];
            bool asJson = args.Any(a => a == "--json");
            ArchiForgeApiClient.DriftAnalysis? drift = await client.GetComparisonDriftAsync(comparisonRecordId);
            if (drift is null)
            {
                Console.WriteLine("Failed to get drift analysis (unauthorized, not found, or request failed).");
                return 1;
            }

            if (asJson)
            {
                string json = JsonSerializer.Serialize(drift, SJsonWriteIndented);
                Console.WriteLine(json);
                return 0;
            }

            Console.WriteLine($"DriftDetected={drift.DriftDetected}");
            Console.WriteLine(drift.Summary);
            foreach (ArchiForgeApiClient.DriftItem item in drift.Items.Take(25))
            {
                Console.WriteLine($"- [{item.Category}] {item.Path}: {item.Description}");
            }
            if (drift.Items.Count > 25)
            {
                Console.WriteLine($"(showing 25 of {drift.Items.Count} items)");
            }

            return 0;
        }

        private static async Task<int> ArchiForge_Comparisons_DiagnosticsAsync(ArchiForgeApiClient client, string[] args)
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
                    asJson = true;
                if (args[i] == "--table")
                    asTable = true;
            }

            ArchiForgeApiClient.ReplayDiagnostics? diagnostics = await client.GetReplayDiagnosticsAsync(limit);
            if (diagnostics is null)
            {
                Console.WriteLine("Failed to get replay diagnostics (unauthorized or request failed).");
                return 1;
            }

            if (asJson)
            {
                string json = JsonSerializer.Serialize(diagnostics, SJsonWriteIndented);
                Console.WriteLine(json);
                return 0;
            }

            if (asTable)
            {
                PrintReplayDiagnosticsTable(diagnostics.RecentReplays);
                return 0;
            }

            foreach (ArchiForgeApiClient.ReplayDiagnosticsEntry e in diagnostics.RecentReplays)
            {
                Console.WriteLine($"{e.TimestampUtc:O} | {e.ComparisonRecordId} | {e.ComparisonType} | {e.Format} | {e.ReplayMode} | Success={e.Success} | {e.DurationMs}ms | MetaOnly={e.MetadataOnly} | Persisted={e.PersistedReplayRecordId} | Err={e.ErrorMessage}");
            }

            return 0;
        }

        private static void PrintReplayDiagnosticsTable(IReadOnlyList<ArchiForgeApiClient.ReplayDiagnosticsEntry> entries)
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
            }).ToList();

            string[] headers = new[] { "TimestampUtc", "ComparisonRecordId", "Type", "Format", "Mode", "Success", "Ms", "MetaOnly", "PersistedReplayRecordId", "Error" };
            rows.Insert(0, headers);

            int[] widths = new int[headers.Length];
            for (int c = 0; c < headers.Length; c++)
                widths[c] = rows.Max(r => r[c].Length);

            for (int i = 0; i < rows.Count; i++)
            {
                string line = string.Join(" | ", rows[i].Select((cell, idx) => cell.PadRight(widths[idx])));
                Console.WriteLine(line);
                if (i == 0)
                    Console.WriteLine(string.Join("-+-", widths.Select(w => new string('-', w))));
            }
        }

        private static int ArchiForge_Dev_Up()
        {
            string? composeDir = FindDockerComposeDirectory();
            if (composeDir is null)
            {
                Console.WriteLine("Error: docker-compose.yml not found. Run from the ArchiForge repo root, or ensure docker-compose.yml exists in the current directory.");
                return 1;
            }

            string composePath = Path.Combine(composeDir, "docker-compose.yml");
            Console.WriteLine($"Starting ArchiForge dev services from {composeDir}...");

            try
            {
                (int exitCode, string output, string error) = RunProcess("docker", $"compose -f \"{composePath}\" up -d", composeDir);
                if (exitCode != 0)
                {
                    (exitCode, output, error) = RunProcess("docker-compose", $"-f \"{composePath}\" up -d", composeDir);
                }

                if (exitCode != 0)
                {
                    Console.WriteLine("Error: Failed to start containers.");
                    if (!string.IsNullOrEmpty(error))
                        Console.WriteLine(error);
                    if (!string.IsNullOrEmpty(output))
                        Console.WriteLine(output);
                    return 1;
                }

                Console.WriteLine();
                Console.WriteLine("Dev services started:");
                Console.WriteLine("  SQL Server: localhost:1433");
                Console.WriteLine("  Azurite:    localhost:10000 (blob), 10001 (queue), 10002 (table)");
                Console.WriteLine("  Redis:      localhost:6379");
                Console.WriteLine();
                Console.WriteLine("Connection string for ArchiForge API (User Secrets or env):");
                Console.WriteLine("  Server=localhost,1433;Database=ArchiForge;User Id=sa;Password=ArchiForge_Dev_Pass123!;TrustServerCertificate=True;");
                Console.WriteLine();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Ensure Docker Desktop is running and 'docker' is in PATH.");
                return 1;
            }
        }

        private static string? FindDockerComposeDirectory()
        {
            string current = Directory.GetCurrentDirectory();
            for (string? dir = current; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
            {
                string composePath = Path.Combine(dir, "docker-compose.yml");
                if (File.Exists(composePath))
                    return dir;
            }
            return null;
        }

        private static (int ExitCode, string StdOut, string StdErr) RunProcess(string fileName, string arguments, string workingDirectory)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using Process? proc = Process.Start(psi);
            if (proc is null)
                return (-1, "", $"Failed to start {fileName}");
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(TimeSpan.FromMinutes(2));
            return (proc.ExitCode, stdout, stderr);
        }

        private static ArchitectureRequest BuildArchitectureRequest(
            ArchiForgeProjectScaffolder.ArchiForgeConfig config,
            string briefContent)
        {
            ArchiForgeProjectScaffolder.ArchitectureSection? arch = config.Architecture;
            ArchitectureRequest request = new ArchitectureRequest
            {
                RequestId = Guid.NewGuid().ToString("N"),
                SystemName = config.ProjectName,
                Description = briefContent,
                Environment = arch?.Environment ?? "prod",
                CloudProvider = ParseCloudProvider(arch?.CloudProvider),
                Constraints = arch?.Constraints ?? [],
                RequiredCapabilities = arch?.RequiredCapabilities ?? [],
                Assumptions = arch?.Assumptions ?? [],
                PriorManifestVersion = arch?.PriorManifestVersion
            };
            return request;
        }

        private static CloudProvider ParseCloudProvider(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CloudProvider.Azure;
            return value.Trim().ToLowerInvariant() switch
            {
                _ => CloudProvider.Azure
            };
        }

        private static async Task<int> ArchiForge_RunAsync(bool quick = false)
        {
            string projectRoot = Directory.GetCurrentDirectory();
            ArchiForgeProjectScaffolder.ArchiForgeConfig config;
            try
            {
                config = ArchiForgeProjectScaffolder.LoadConfig(projectRoot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            string briefPath = Path.Combine(projectRoot, config.Inputs.Brief);
            if (!File.Exists(briefPath))
            {
                Console.WriteLine($"Error: Brief file not found at {config.Inputs.Brief}");
                return 1;
            }

            string briefContent = (await File.ReadAllTextAsync(briefPath)).Trim();
            if (briefContent.Length < 10)
            {
                Console.WriteLine("Error: Brief must be at least 10 characters (API requirement).");
                return 1;
            }

            ArchitectureRequest request = BuildArchitectureRequest(config, briefContent);

            string baseUrl = GetBaseUrl(config);
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);

            Console.WriteLine($"Submitting request to {baseUrl}...");

            ArchiForgeApiClient.CreateRunResult result = await client.CreateRunAsync(request);

            if (!result.Success)
            {
                Console.WriteLine($"Error: {result.Error}");
                return 1;
            }

            ArchiForgeApiClient.CreateRunResponse resp = result.Response!;

            string outputsDir = Path.Combine(projectRoot, config.Outputs.LocalCacheDir);
            Directory.CreateDirectory(outputsDir);
            string summaryPath = Path.Combine(outputsDir, "run-summary.json");
            WriteRunSummary(summaryPath, baseUrl, resp.Run.RunId, resp.Run.RequestId, resp.Run.Status, resp.Run.CreatedUtc, resp.Tasks, manifestVersion: null);

            Console.WriteLine();
            Console.WriteLine($"Run created: {resp.Run.RunId}");
            Console.WriteLine($"Status: {resp.Run.Status}");
            Console.WriteLine($"run-summary.json written to {summaryPath}");
            Console.WriteLine();
            Console.WriteLine("Tasks:");
            foreach (ArchiForgeApiClient.AgentTaskInfo task in resp.Tasks)
            {
                AgentType agentType = (AgentType)task.AgentType;
                Console.WriteLine($"  - {agentType}: {task.Objective}");
            }
            Console.WriteLine();
            if (quick)
            {
                Console.WriteLine("Quick mode: seeding fake results and committing...");
                ArchiForgeApiClient.SeedFakeResultsResult? seedResult = await client.SeedFakeResultsAsync(resp.Run.RunId);
                if (seedResult is null || !seedResult.Success)
                {
                    Console.WriteLine($"Warning: Seed failed. {seedResult?.Error ?? "Unknown"}");
                    Console.WriteLine("Note: Seed is only available when the API runs in Development.");
                    Console.WriteLine($"Continue with: archiforge seed {resp.Run.RunId} then archiforge commit {resp.Run.RunId}");
                    return 0;
                }
                Console.WriteLine($"Seeded {seedResult.ResultCount} fake results.");
                ArchiForgeApiClient.CommitRunResult? commitResult = await client.CommitRunAsync(resp.Run.RunId);
                if (commitResult is null || !commitResult.Success)
                {
                    Console.WriteLine($"Error: Commit failed. {commitResult?.Error ?? "Unknown"}");
                    return 1;
                }
                string version = commitResult.Response?.Manifest.Metadata.ManifestVersion ?? "unknown";
                Console.WriteLine($"Committed. Manifest version: {version}");
                WriteRunSummary(summaryPath, baseUrl, resp.Run.RunId, resp.Run.RequestId, resp.Run.Status, resp.Run.CreatedUtc, resp.Tasks, version);
                Console.WriteLine($"Use 'archiforge artifacts {resp.Run.RunId}' to view the manifest.");
                return 0;
            }
            Console.WriteLine($"Next: Submit agent results, then commit. Use 'archiforge status {resp.Run.RunId}' to check progress.");
            return 0;
        }

        private static void WriteRunSummary(string path, string apiBaseUrl, string runId, string requestId, int status, DateTime createdUtc, IReadOnlyList<ArchiForgeApiClient.AgentTaskInfo> tasks, string? manifestVersion)
        {
#pragma warning disable IDE0300 // Simplify collection initialization
#pragma warning disable IDE0301 // Simplify collection initialization
            var summary = new
            {
                runId,
                requestId,
                status,
                createdUtc = createdUtc.ToString("O"),
                manifestVersion,
                apiBaseUrl,
                tasks = tasks.Select(t => new { t.TaskId, agentType = (AgentType)t.AgentType, t.Objective }).ToArray(),
                artifactUris = manifestVersion != null ? new[] { $"{apiBaseUrl}/v1/architecture/manifest/{manifestVersion}" } : Array.Empty<string>()
            };
#pragma warning restore IDE0301 // Simplify collection initialization
#pragma warning restore IDE0300 // Simplify collection initialization
            string json = JsonSerializer.Serialize(summary, SJsonWriteIndented);
            File.WriteAllText(path, json);
        }

        private static async Task<int> ArchiForge_StatusAsync(string runId)
        {
            string baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);

            ArchiForgeApiClient.GetRunResult? run = await client.GetRunAsync(runId);
            if (run is null)
            {
                Console.WriteLine($"Run '{runId}' not found. Ensure the ArchiForge API is running at {baseUrl}.");
                return 1;
            }

            ArchiForgeApiClient.RunInfo r = run.Run;
            ArchitectureRunStatus status = (ArchitectureRunStatus)r.Status;
            Console.WriteLine($"Run: {r.RunId}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Created: {r.CreatedUtc:O}");
            if (r.CompletedUtc.HasValue)
                Console.WriteLine($"Completed: {r.CompletedUtc:O}");
            if (!string.IsNullOrEmpty(r.CurrentManifestVersion))
                Console.WriteLine($"Manifest version: {r.CurrentManifestVersion}");
            Console.WriteLine();
            Console.WriteLine("Tasks:");
            foreach (ArchiForgeApiClient.AgentTaskInfo task in run.Tasks)
            {
                AgentType agentType = (AgentType)task.AgentType;
                AgentTaskStatus taskStatus = (AgentTaskStatus)task.Status;
                Console.WriteLine($"  {agentType}: {taskStatus} - {task.Objective}");
            }
            Console.WriteLine($"Results: {run.Results.Count} submitted");
            return 0;
        }

        private static async Task<int> ArchiForge_SubmitAsync(string runId, string resultFilePath)
        {
            string baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            if (!File.Exists(resultFilePath))
            {
                Console.WriteLine($"Error: File not found: {resultFilePath}");
                return 1;
            }

            AgentResult result;
            try
            {
                string json = await File.ReadAllTextAsync(resultFilePath);
                result = JsonSerializer.Deserialize<AgentResult>(json, SJsonDeserializeAgentResult) ?? new AgentResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Invalid result JSON. {ex.Message}");
                return 1;
            }

            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);
            ArchiForgeApiClient.SubmitResultResult? submitResult = await client.SubmitAgentResultAsync(runId, result);
            if (submitResult is null || !submitResult.Success)
            {
                Console.WriteLine($"Error: {submitResult?.Error ?? "Submit failed"}");
                return 1;
            }

            Console.WriteLine($"Result submitted: {submitResult.ResultId}");
            Console.WriteLine($"Use 'archiforge status {runId}' to check progress, then 'archiforge commit {runId}' when all results are in.");
            return 0;
        }

        private static async Task<int> ArchiForge_CommitAsync(string runId)
        {
            string baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);

            ArchiForgeApiClient.CommitRunResult? result = await client.CommitRunAsync(runId);
            if (result is null || !result.Success)
            {
                Console.WriteLine($"Error: {result?.Error ?? "Commit failed"}");
                return 1;
            }

            ArchiForgeApiClient.CommitRunResponse resp = result.Response!;
            string version = resp.Manifest.Metadata.ManifestVersion;
            Console.WriteLine($"Committed: {resp.Manifest.SystemName}");
            Console.WriteLine($"Manifest version: {version}");
            if (resp.Warnings.Count > 0)
            {
                Console.WriteLine("Warnings:");
                foreach (string w in resp.Warnings)
                    Console.WriteLine($"  - {w}");
            }
            Console.WriteLine();
            Console.WriteLine($"Use 'archiforge artifacts {runId}' to view the manifest.");
            return 0;
        }

        private static async Task<int> ArchiForge_SeedAsync(string runId)
        {
            string baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);

            ArchiForgeApiClient.SeedFakeResultsResult? result = await client.SeedFakeResultsAsync(runId);
            if (result is null || !result.Success)
            {
                Console.WriteLine($"Error: {result?.Error ?? "Seed failed"}");
                Console.WriteLine("Note: seed-fake-results is only available when the API runs in Development.");
                return 1;
            }

            Console.WriteLine($"Seeded {result.ResultCount} fake results for run {runId}");
            Console.WriteLine($"Use 'archiforge commit {runId}' to produce the manifest.");
            return 0;
        }

        private static async Task<int> ArchiForge_ArtifactsAsync(string runId, bool save = false)
        {
            ArchiForgeProjectScaffolder.ArchiForgeConfig? config = TryLoadConfigFromCwd();
            string baseUrl = GetBaseUrl(config);
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            ArchiForgeApiClient client = new ArchiForgeApiClient(baseUrl);

            ArchiForgeApiClient.GetRunResult? run = await client.GetRunAsync(runId);
            if (run is null)
            {
                Console.WriteLine($"Run '{runId}' not found. Ensure the ArchiForge API is running at {baseUrl}.");
                return 1;
            }

            string? version = run.Run.CurrentManifestVersion;
            if (string.IsNullOrEmpty(version))
            {
                Console.WriteLine($"Run {runId} has not been committed. Submit all agent results and call commit first.");
                return 1;
            }

            object? manifest = await client.GetManifestAsync(version);
            if (manifest is null)
            {
                Console.WriteLine($"Manifest '{version}' not found.");
                return 1;
            }

            string json = JsonSerializer.Serialize(manifest, SJsonWriteIndented);
            Console.WriteLine($"Manifest version: {version}");
            Console.WriteLine();

            if (save && config is not null)
            {
                try
                {
                    string projectRoot = Directory.GetCurrentDirectory();
                    string outputsDir = Path.Combine(projectRoot, config.Outputs.LocalCacheDir);
                    Directory.CreateDirectory(outputsDir);
                    string fileName = $"manifest-{version}.json";
                    string filePath = Path.Combine(outputsDir, fileName);
                    await File.WriteAllTextAsync(filePath, json);
                    Console.WriteLine($"Saved to {filePath}");
                    Console.WriteLine($"URI: {baseUrl}/v1/architecture/manifest/{version}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not save manifest to outputs: {ex.Message}");
                    Console.WriteLine(json);
                }
            }
            else
            {
                Console.WriteLine(json);
            }
            return 0;
        }
    }
}
