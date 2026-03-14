using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a command. Available commands: new, dev up, run, status <runId>, commit <runId>, artifacts <runId>");
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
                    return await ArchiForge_RunAsync();

                case "status":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archiforge status <runId>");
                        return 1;
                    }
                    return await ArchiForge_StatusAsync(args[1]);

                case "commit":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archiforge commit <runId>");
                        return 1;
                    }
                    return await ArchiForge_CommitAsync(args[1]);

                case "artifacts":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archiforge artifacts <runId>");
                        return 1;
                    }
                    return await ArchiForge_ArtifactsAsync(args[1]);

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

        private static async Task<int> ArchiForge_RunAsync()
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

            var baseUrl = ArchiForgeApiClient.GetDefaultBaseUrl();
            var client = new ArchiForgeApiClient(baseUrl);

            Console.WriteLine($"Submitting request to {baseUrl}...");

            var result = await client.CreateRunAsync(request);

            if (!result.Success)
            {
                Console.WriteLine($"Error: {result.Error}");
                return 1;
            }

            var resp = result.Response!;
            Console.WriteLine();
            Console.WriteLine($"Run created: {resp.Run.RunId}");
            Console.WriteLine($"Status: {resp.Run.Status}");
            Console.WriteLine();
            Console.WriteLine("Tasks:");
            foreach (var task in resp.Tasks)
            {
                var agentType = (AgentType)task.AgentType;
                Console.WriteLine($"  - {agentType}: {task.Objective}");
            }
            Console.WriteLine();
            Console.WriteLine($"Next: Submit agent results, then commit. Use 'archiforge status {resp.Run.RunId}' to check progress.");
            return 0;
        }

        private static async Task<int> ArchiForge_StatusAsync(string runId)
        {
            var baseUrl = ArchiForgeApiClient.GetDefaultBaseUrl();
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

        private static async Task<int> ArchiForge_CommitAsync(string runId)
        {
            var baseUrl = ArchiForgeApiClient.GetDefaultBaseUrl();
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

        private static async Task<int> ArchiForge_ArtifactsAsync(string runId)
        {
            var baseUrl = ArchiForgeApiClient.GetDefaultBaseUrl();
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

            Console.WriteLine($"Manifest version: {version}");
            Console.WriteLine();
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
    }
}

