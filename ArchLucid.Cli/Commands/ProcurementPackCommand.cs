using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     <c>archlucid procurement-pack</c> — builds the buyer procurement ZIP via
///     <c>scripts/build_procurement_pack.py</c>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Process orchestration; validated by ProcurementPackCommandTests + CI.")]
internal static class ProcurementPackCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        bool dryRun = false;
        string? outZip = null;

        for (int i = 0; i < args.Length; i++)
        {
            string token = args[i];

            if (string.Equals(token, "--dry-run", StringComparison.Ordinal))
            {
                dryRun = true;

                continue;
            }

            if (string.Equals(token, "--out", StringComparison.Ordinal))
            {
                if (i + 1 >= args.Length)
                {
                    await Console.Error.WriteLineAsync("Missing value for --out.");

                    return CliExitCode.UsageError;
                }

                outZip = args[++i].Trim();

                continue;
            }

            await Console.Error.WriteLineAsync($"Unexpected argument: {token}");

            return CliExitCode.UsageError;
        }

        string? repoRoot = CliRepositoryRootResolver.TryResolveRepositoryRoot();

        if (repoRoot is null || !Directory.Exists(repoRoot))
        {
            await Console.Error.WriteLineAsync(
                "Could not locate repository root (expected docs/go-to-market/MARKETPLACE_PUBLICATION.md). "
                + "Run from the repo tree.");

            return CliExitCode.UsageError;
        }

        string scriptPath = Path.Combine(repoRoot, "scripts", "build_procurement_pack.py");

        if (!File.Exists(scriptPath))
        {
            await Console.Error.WriteLineAsync($"Missing build script: {scriptPath}");

            return CliExitCode.OperationFailed;
        }

        string pythonExe = ResolvePythonExecutable();

        ProcessStartInfo psi = new()
        {
            FileName = pythonExe,
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        psi.ArgumentList.Add(scriptPath);

        if (dryRun)
            psi.ArgumentList.Add("--dry-run");

        if (!string.IsNullOrWhiteSpace(outZip))
        {
            psi.ArgumentList.Add("--out");
            psi.ArgumentList.Add(Path.GetFullPath(outZip));
        }

        using Process process = new();
        process.StartInfo = psi;

        try
        {
            if (!process.Start())
            {
                await Console.Error.WriteLineAsync("Failed to start Python build script.");

                return CliExitCode.OperationFailed;
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(
                $"Could not execute '{pythonExe}'. Install Python 3 and ensure it is on PATH, or set ARCHLUCID_PYTHON to the interpreter path. ({ex.Message})");

            return CliExitCode.OperationFailed;
        }

        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (stdout.Length > 0)
            await Console.Out.WriteLineAsync(stdout.TrimEnd());

        if (stderr.Length > 0)
            await Console.Error.WriteLineAsync(stderr.TrimEnd());

        return process.ExitCode == 0 ? CliExitCode.Success : CliExitCode.OperationFailed;
    }

    private static string ResolvePythonExecutable()
    {
        string? fromEnv = Environment.GetEnvironmentVariable("ARCHLUCID_PYTHON");

        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "python3";
    }
}
