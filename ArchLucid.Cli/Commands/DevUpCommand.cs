using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Cli;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI dev up invokes Docker Compose from the host; environment-dependent; exercised manually.")]
internal static class DevUpCommand
{
    public static Task<int> RunAsync()
    {
        string? composeDir = FindDockerComposeDirectory();

        if (composeDir is null)
        {
            Console.WriteLine(
                "Error: docker-compose.yml not found. Run from the ArchLucid repo root, or ensure docker-compose.yml exists in the current directory.");

            return Task.FromResult(CliExitCode.UsageError);
        }

        string composePath = Path.Combine(composeDir, "docker-compose.yml");
        Console.WriteLine($"Starting ArchLucid dev services from {composeDir}...");

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
                {
                    Console.WriteLine(error);
                }

                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine(output);
                }

                return Task.FromResult(CliExitCode.OperationFailed);
            }

            Console.WriteLine();
            Console.WriteLine("Dev services started:");
            Console.WriteLine("  SQL Server: localhost:1433");
            Console.WriteLine("  Azurite:    localhost:10000 (blob), 10001 (queue), 10002 (table)");
            Console.WriteLine("  Redis:      localhost:6379");
            Console.WriteLine();
            Console.WriteLine("Connection string for ArchLucid API (User Secrets or env):");
            Console.WriteLine(
                "  Server=localhost,1433;Database=ArchLucid;User Id=sa;Password=ArchLucid_Dev_Pass123!;TrustServerCertificate=True;");
            Console.WriteLine();

            return Task.FromResult(CliExitCode.Success);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Ensure Docker Desktop is running and 'docker' is in PATH.");

            return Task.FromResult(CliExitCode.OperationFailed);
        }
    }

    private static string? FindDockerComposeDirectory()
    {
        string current = Directory.GetCurrentDirectory();

        for (string? dir = current; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
        {
            string composePath = Path.Combine(dir, "docker-compose.yml");

            if (File.Exists(composePath))
            {
                return dir;
            }
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
        {
            return (-1, "", $"Failed to start {fileName}");
        }

        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(TimeSpan.FromMinutes(2));

        return (proc.ExitCode, stdout, stderr);
    }
}
