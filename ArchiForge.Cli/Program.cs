using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge
{
    public static class Program
    {
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

            var command = args[0];

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
                    var quick = args.Length > 1 && args[1] == "--quick";
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
                    var saveArtifacts = args.Length > 2 && args[2] == "--save";
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
            var scaffoldOptions = new ArchiForgeProjectScaffolder.ScaffoldOptions
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
            var client = new ArchiForgeApiClient(baseUrl);
            if (await client.CheckHealthAsync(ct))
                return true;
            Console.WriteLine($"Cannot connect to ArchiForge API at {baseUrl}");
            Console.WriteLine("Ensure the API is running: dotnet run --project ArchiForge.Api");
            Console.WriteLine("Or set apiUrl in archiforge.json / ARCHIFORGE_API_URL environment variable.");
            return false;
        }

        private static async Task<int> ArchiForge_HealthAsync()
        {
            var baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            var client = new ArchiForgeApiClient(baseUrl);
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
                Console.WriteLine("Usage: archiforge comparisons list [--type <type>] [--left-run <runId>] [--right-run <runId>] [--tag <tag>] [--skip <n>] [--limit <n>]");
                Console.WriteLine("   or: archiforge comparisons replay <comparisonRecordId> [--format <markdown|html|docx|pdf>] [--mode <artifact|regenerate|verify>] [--profile <profile>] [--persist] [--out <path>] [--force]");
                Console.WriteLine("   or: archiforge comparisons drift <comparisonRecordId>");
                Console.WriteLine("   or: archiforge comparisons diagnostics [--limit <n>]");
                Console.WriteLine("   or: archiforge comparisons tag <comparisonRecordId> [--label <label>] [--tag <t>]...");
                return 1;
            }

            var sub = args[0];
            var config = TryLoadConfigFromCwd();
            var baseUrl = GetBaseUrl(config);
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            var client = new ArchiForgeApiClient(baseUrl);

            switch (sub)
            {
                case "list":
                    return await ArchiForge_Comparisons_ListAsync(client, args.Skip(1).ToArray());
                case "replay":
                    return await ArchiForge_Comparisons_ReplayAsync(client, args.Skip(1).ToArray());
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
            string? tag = null;
            int skip = 0;
            int limit = 20;

            for (var i = 0; i < args.Length; i++)
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
                    case "--tag" when i + 1 < args.Length:
                        tag = args[++i];
                        break;
                    case "--skip" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedSkip):
                        skip = parsedSkip;
                        i++;
                        break;
                    case "--limit" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsed):
                        limit = parsed;
                        i++;
                        break;
                }
            }

            var result = await client.SearchComparisonsAsync(type, leftRun, rightRun, tag, skip, limit);
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

            foreach (var r in result.Records)
            {
                var labelPart = string.IsNullOrEmpty(r.Label) ? "" : $" Label={r.Label}";
                var tagsPart = r.Tags.Count == 0 ? "" : " Tags=[" + string.Join(",", r.Tags) + "]";
                Console.WriteLine($"{r.CreatedUtc:O} | {r.ComparisonRecordId} | {r.ComparisonType} | LeftRun={r.LeftRunId} RightRun={r.RightRunId}{labelPart}{tagsPart}");
            }

            return 0;
        }

        private static async Task<int> ArchiForge_Comparisons_TagAsync(ArchiForgeApiClient client, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: archiforge comparisons tag <comparisonRecordId> [--label <label>] [--tag <t>]...");
                return 1;
            }

            var comparisonRecordId = args[0];
            string? label = null;
            var tags = new List<string>();

            for (var i = 1; i < args.Length; i++)
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

            var ok = await client.UpdateComparisonRecordAsync(comparisonRecordId, label, tags);
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

            var comparisonRecordId = args[0];
            var format = "markdown";
            var mode = "artifact";
            string? profile = null;
            var persist = false;
            string? outPath = null;
            var force = false;

            for (var i = 1; i < args.Length; i++)
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

            var ok = await client.ReplayComparisonToFileAsync(comparisonRecordId, format, mode, profile, persist, outPath, force);
            return ok ? 0 : 1;
        }

        private static async Task<int> ArchiForge_Comparisons_DriftAsync(ArchiForgeApiClient client, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: archiforge comparisons drift <comparisonRecordId>");
                return 1;
            }

            var comparisonRecordId = args[0];
            var drift = await client.GetComparisonDriftAsync(comparisonRecordId);
            if (drift is null)
            {
                Console.WriteLine("Failed to get drift analysis (unauthorized, not found, or request failed).");
                return 1;
            }

            Console.WriteLine($"DriftDetected={drift.DriftDetected}");
            Console.WriteLine(drift.Summary);
            foreach (var item in drift.Items.Take(25))
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
            var limit = 20;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsed))
                {
                    limit = parsed;
                    i++;
                }
            }

            var diagnostics = await client.GetReplayDiagnosticsAsync(limit);
            if (diagnostics is null)
            {
                Console.WriteLine("Failed to get replay diagnostics (unauthorized or request failed).");
                return 1;
            }

            foreach (var e in diagnostics.RecentReplays)
            {
                Console.WriteLine($"{e.TimestampUtc:O} | {e.ComparisonRecordId} | {e.ComparisonType} | {e.Format} | {e.ReplayMode} | Success={e.Success} | {e.DurationMs}ms | MetaOnly={e.MetadataOnly} | Persisted={e.PersistedReplayRecordId} | Err={e.ErrorMessage}");
            }

            return 0;
        }

        private static int ArchiForge_Dev_Up()
        {
            var composeDir = FindDockerComposeDirectory();
            if (composeDir is null)
            {
                Console.WriteLine("Error: docker-compose.yml not found. Run from the ArchiForge repo root, or ensure docker-compose.yml exists in the current directory.");
                return 1;
            }

            var composePath = Path.Combine(composeDir, "docker-compose.yml");
            Console.WriteLine($"Starting ArchiForge dev services from {composeDir}...");

            try
            {
                var (exitCode, output, error) = RunProcess("docker", $"compose -f \"{composePath}\" up -d", composeDir);
                if (exitCode != 0)
                {
                    (exitCode, output, error) = RunProcess("docker-compose", $"-f \"{composePath}\" up -d", composeDir);
                }

                if (exitCode != 0)
                {
                    Console.WriteLine($"Error: Failed to start containers.");
                    if (!string.IsNullOrEmpty(error)) Console.WriteLine(error);
                    if (!string.IsNullOrEmpty(output)) Console.WriteLine(output);
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
            var current = Directory.GetCurrentDirectory();
            for (var dir = current; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
            {
                var composePath = Path.Combine(dir, "docker-compose.yml");
                if (File.Exists(composePath))
                    return dir;
            }
            return null;
        }

        private static (int ExitCode, string StdOut, string StdErr) RunProcess(string fileName, string arguments, string workingDirectory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc is null)
                return (-1, "", $"Failed to start {fileName}");
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(TimeSpan.FromMinutes(2));
            return (proc.ExitCode, stdout, stderr);
        }

        private static ArchitectureRequest BuildArchitectureRequest(
            ArchiForgeProjectScaffolder.ArchiForgeConfig config,
            string briefContent)
        {
            var arch = config.Architecture;
            var request = new ArchitectureRequest
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
            if (string.IsNullOrWhiteSpace(value)) return CloudProvider.Azure;
            return value.Trim().ToLowerInvariant() switch
            {
                "azure" => CloudProvider.Azure,
                "1" => CloudProvider.Azure,
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

            var briefPath = Path.Combine(projectRoot, config.Inputs.Brief);
            if (!File.Exists(briefPath))
            {
                Console.WriteLine($"Error: Brief file not found at {config.Inputs.Brief}");
                return 1;
            }

            var briefContent = File.ReadAllText(briefPath).Trim();
            if (briefContent.Length < 10)
            {
                Console.WriteLine("Error: Brief must be at least 10 characters (API requirement).");
                return 1;
            }

            var request = BuildArchitectureRequest(config, briefContent);

            var baseUrl = GetBaseUrl(config);
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            var client = new ArchiForgeApiClient(baseUrl);

            Console.WriteLine($"Submitting request to {baseUrl}...");

            var result = await client.CreateRunAsync(request);

            if (!result.Success)
            {
                Console.WriteLine($"Error: {result.Error}");
                return 1;
            }

            var resp = result.Response!;

            var outputsDir = Path.Combine(projectRoot, config.Outputs.LocalCacheDir);
            Directory.CreateDirectory(outputsDir);
            var summaryPath = Path.Combine(outputsDir, "run-summary.json");
            WriteRunSummary(summaryPath, baseUrl, resp.Run.RunId, resp.Run.RequestId, resp.Run.Status, resp.Run.CreatedUtc, resp.Tasks, manifestVersion: null);

            Console.WriteLine();
            Console.WriteLine($"Run created: {resp.Run.RunId}");
            Console.WriteLine($"Status: {resp.Run.Status}");
            Console.WriteLine($"run-summary.json written to {summaryPath}");
            Console.WriteLine();
            Console.WriteLine("Tasks:");
            foreach (var task in resp.Tasks)
            {
                var agentType = (AgentType)task.AgentType;
                Console.WriteLine($"  - {agentType}: {task.Objective}");
            }
            Console.WriteLine();
            if (quick)
            {
                Console.WriteLine("Quick mode: seeding fake results and committing...");
                var seedResult = await client.SeedFakeResultsAsync(resp.Run.RunId);
                if (seedResult is null || !seedResult.Success)
                {
                    Console.WriteLine($"Warning: Seed failed. {seedResult?.Error ?? "Unknown"}");
                    Console.WriteLine("Note: Seed is only available when the API runs in Development.");
                    Console.WriteLine($"Continue with: archiforge seed {resp.Run.RunId} then archiforge commit {resp.Run.RunId}");
                    return 0;
                }
                Console.WriteLine($"Seeded {seedResult.ResultCount} fake results.");
                var commitResult = await client.CommitRunAsync(resp.Run.RunId);
                if (commitResult is null || !commitResult.Success)
                {
                    Console.WriteLine($"Error: Commit failed. {commitResult?.Error ?? "Unknown"}");
                    return 1;
                }
                var version = commitResult.Response?.Manifest?.Metadata?.ManifestVersion ?? "unknown";
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
            var json = System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private static async Task<int> ArchiForge_StatusAsync(string runId)
        {
            var baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            var client = new ArchiForgeApiClient(baseUrl);

            var run = await client.GetRunAsync(runId);
            if (run is null)
            {
                Console.WriteLine($"Run '{runId}' not found. Ensure the ArchiForge API is running at {baseUrl}.");
                return 1;
            }

            var r = run.Run;
            var status = (ArchitectureRunStatus)r.Status;
            Console.WriteLine($"Run: {r.RunId}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Created: {r.CreatedUtc:O}");
            if (r.CompletedUtc.HasValue)
                Console.WriteLine($"Completed: {r.CompletedUtc:O}");
            if (!string.IsNullOrEmpty(r.CurrentManifestVersion))
                Console.WriteLine($"Manifest version: {r.CurrentManifestVersion}");
            Console.WriteLine();
            Console.WriteLine("Tasks:");
            foreach (var task in run.Tasks)
            {
                var agentType = (AgentType)task.AgentType;
                var taskStatus = (AgentTaskStatus)task.Status;
                Console.WriteLine($"  {agentType}: {taskStatus} - {task.Objective}");
            }
            Console.WriteLine($"Results: {run.Results.Count} submitted");
            return 0;
        }

        private static async Task<int> ArchiForge_SubmitAsync(string runId, string resultFilePath)
        {
            var baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
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
                var json = File.ReadAllText(resultFilePath);
                result = System.Text.Json.JsonSerializer.Deserialize<AgentResult>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }) ?? new AgentResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Invalid result JSON. {ex.Message}");
                return 1;
            }

            var client = new ArchiForgeApiClient(baseUrl);
            var submitResult = await client.SubmitAgentResultAsync(runId, result);
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
            var baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            var client = new ArchiForgeApiClient(baseUrl);

            var result = await client.CommitRunAsync(runId);
            if (result is null || !result.Success)
            {
                Console.WriteLine($"Error: {result?.Error ?? "Commit failed"}");
                return 1;
            }

            var resp = result.Response!;
            var version = resp.Manifest?.Metadata?.ManifestVersion ?? "unknown";
            Console.WriteLine($"Committed: {resp.Manifest?.SystemName ?? runId}");
            Console.WriteLine($"Manifest version: {version}");
            if (resp.Warnings.Count > 0)
            {
                Console.WriteLine("Warnings:");
                foreach (var w in resp.Warnings)
                    Console.WriteLine($"  - {w}");
            }
            Console.WriteLine();
            Console.WriteLine($"Use 'archiforge artifacts {runId}' to view the manifest.");
            return 0;
        }

        private static async Task<int> ArchiForge_SeedAsync(string runId)
        {
            var baseUrl = GetBaseUrl(TryLoadConfigFromCwd());
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            var client = new ArchiForgeApiClient(baseUrl);

            var result = await client.SeedFakeResultsAsync(runId);
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
            var config = TryLoadConfigFromCwd();
            var baseUrl = GetBaseUrl(config);
            if (!await EnsureApiConnectedAsync(baseUrl))
                return 1;

            var client = new ArchiForgeApiClient(baseUrl);

            var run = await client.GetRunAsync(runId);
            if (run is null)
            {
                Console.WriteLine($"Run '{runId}' not found. Ensure the ArchiForge API is running at {baseUrl}.");
                return 1;
            }

            var version = run.Run.CurrentManifestVersion;
            if (string.IsNullOrEmpty(version))
            {
                Console.WriteLine($"Run {runId} has not been committed. Submit all agent results and call commit first.");
                return 1;
            }

            var manifest = await client.GetManifestAsync(version);
            if (manifest is null)
            {
                Console.WriteLine($"Manifest '{version}' not found.");
                return 1;
            }

            var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"Manifest version: {version}");
            Console.WriteLine();

            if (save && config is not null)
            {
                try
                {
                    var projectRoot = Directory.GetCurrentDirectory();
                    var outputsDir = Path.Combine(projectRoot, config.Outputs.LocalCacheDir);
                    Directory.CreateDirectory(outputsDir);
                    var fileName = $"manifest-{version}.json";
                    var filePath = Path.Combine(outputsDir, fileName);
                    File.WriteAllText(filePath, json);
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
