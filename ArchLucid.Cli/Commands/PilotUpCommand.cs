using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Cli.Commands;

/// <summary>
/// Starts the Docker demo stack (compose + demo overlay, full-stack profile) for evaluator pilots.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Invokes docker compose and polls a live API; environment-dependent.")]
internal static class PilotUpCommand
{
    private const string ReadyUrl = "http://127.0.0.1:5000/health/ready";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ReadyDeadline = TimeSpan.FromSeconds(120);

    public static async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        string? composeDir = FindDockerComposeDirectory();

        if (composeDir is null)
        {
            Console.WriteLine(
                "Error: docker-compose.yml not found. Run from the ArchLucid repo root, or ensure docker-compose.yml exists in the current directory.");

            return CliExitCode.UsageError;
        }

        string composeBase = Path.Combine(composeDir, "docker-compose.yml");
        string composeDemo = Path.Combine(composeDir, "docker-compose.demo.yml");

        if (!File.Exists(composeDemo))
        {
            Console.WriteLine($"Error: Expected demo overlay at {composeDemo} (see scripts/demo-start.ps1).");

            return CliExitCode.UsageError;
        }

        Console.WriteLine($"Starting pilot stack from {composeDir} (full-stack + demo overlay)...");

        (int exitCode, string stdout, string stderr) = RunProcess(
            "docker",
            $"compose -f \"{composeBase}\" -f \"{composeDemo}\" --profile full-stack up -d --build",
            composeDir);

        if (exitCode != 0)
        {
            Console.WriteLine("Error: docker compose up failed.");

            if (!string.IsNullOrEmpty(stderr))

                Console.WriteLine(stderr);


            if (!string.IsNullOrEmpty(stdout))

                Console.WriteLine(stdout);


            return CliExitCode.OperationFailed;
        }

        Console.WriteLine("Waiting for API readiness (GET /health/ready on http://127.0.0.1:5000)...");
        DateTime deadline = DateTime.UtcNow + ReadyDeadline;
        bool ready = false;

        using HttpClient probe = new()
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                using HttpResponseMessage response = await probe.GetAsync(ReadyUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    ready = true;

                    break;
                }
            }
            catch (HttpRequestException)
            {
                // still starting
            }
            catch (TaskCanceledException)
            {
                // timeout on single probe
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        if (!ready)
        {
            Console.WriteLine($"Timed out after {ReadyDeadline.TotalSeconds:n0}s waiting for {ReadyUrl}.");
            Console.WriteLine("Check logs: docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack logs api");

            return CliExitCode.OperationFailed;
        }

        Console.WriteLine();
        Console.WriteLine("Pilot stack is up.");
        Console.WriteLine("  Operator UI:  http://localhost:3000/runs/new");
        Console.WriteLine("  API (Swagger): http://localhost:5000/swagger");
        Console.WriteLine("  Health:        http://localhost:5000/health/ready");
        Console.WriteLine();
        Console.WriteLine("Demo seed runs at API startup when the demo overlay is applied (Demo__SeedOnStartup=true).");
        Console.WriteLine("Agent execution uses the simulator (no Azure OpenAI keys required for this path).");

        return CliExitCode.Success;
    }

    internal static string? FindDockerComposeDirectory()
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
        ProcessStartInfo psi = new()
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
        proc.WaitForExit(TimeSpan.FromMinutes(10));

        return (proc.ExitCode, stdout, stderr);
    }
}
