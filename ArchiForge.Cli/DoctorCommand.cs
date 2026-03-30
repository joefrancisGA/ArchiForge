namespace ArchiForge.Cli;

/// <summary>
/// Operator-facing readiness diagnostics: local project layout and API <c>/health/live</c> + <c>/health/ready</c>.
/// </summary>
internal static class DoctorCommand
{
    public static async Task<int> RunAsync(ArchiForgeProjectScaffolder.ArchiForgeConfig? config, CancellationToken ct = default)
    {
        Console.WriteLine("ArchiForge doctor — local checks and API readiness");
        Console.WriteLine();

        RunLocalProjectChecks(config);

        string baseUrl = ArchiForgeApiClient.ResolveBaseUrl(config);
        string? urlError = ArchiForgeApiClient.GetInvalidApiBaseUrlReason(baseUrl);

        if (urlError is not null)
        {
            Console.Error.WriteLine("[ArchiForge CLI] " + urlError);

            return 1;
        }

        Console.WriteLine("--- ArchiForge API ---");
        Console.WriteLine($"Base URL: {baseUrl}");

        ArchiForgeApiClient client = new(baseUrl);

        await PrintProbeAsync(client, "/health/live", "Liveness (/health/live)", ct);
        bool readyOk = await PrintProbeAsync(client, "/health/ready", "Readiness (/health/ready)", ct);

        (int aggregateCode, string aggregateBody) = await client.GetHealthProbeAsync("/health", ct);
        Console.WriteLine();
        Console.WriteLine($"Combined health (/health) HTTP {aggregateCode}");
        Console.WriteLine(TruncateForDisplay(aggregateBody, maxChars: 4000));

        if (!readyOk)
        {
            Console.WriteLine();
            Console.WriteLine("Readiness failed: fix the checks above before relying on this environment.");

            return 1;
        }

        if (aggregateCode < 200 || aggregateCode >= 300)
        {
            Console.WriteLine();
            Console.WriteLine("Combined /health did not return 2xx; review JSON above.");

            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Doctor finished: readiness OK.");

        return 0;
    }

    private static void RunLocalProjectChecks(ArchiForgeProjectScaffolder.ArchiForgeConfig? config)
    {
        Console.WriteLine("--- Local project ---");
        string cwd = Directory.GetCurrentDirectory();

        if (config is null)
        {
            Console.WriteLine(
                $"No archiforge.json in '{cwd}' (skipped local outputs/brief checks). API checks still run.");

            Console.WriteLine();

            return;
        }

        Console.WriteLine($"Project: {config.ProjectName} (schema {config.SchemaVersion})");

        string briefPath = Path.Combine(cwd, config.Inputs.Brief);
        if (File.Exists(briefPath))
        {
            Console.WriteLine($"Brief: OK — {config.Inputs.Brief}");
        }
        else
        {
            Console.WriteLine($"Brief: MISSING — expected file at {config.Inputs.Brief} (needed for 'archiforge run').");
        }

        string outputsDir = Path.Combine(cwd, config.Outputs.LocalCacheDir);

        try
        {
            Directory.CreateDirectory(outputsDir);
            string probe = Path.Combine(outputsDir, ".archiforge-write-probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            Console.WriteLine($"Outputs dir: OK — {config.Outputs.LocalCacheDir} is writable");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Outputs dir: FAIL — cannot use '{config.Outputs.LocalCacheDir}': {ex.Message}");
        }

        Console.WriteLine();
    }

    private static async Task<bool> PrintProbeAsync(
        ArchiForgeApiClient client,
        string path,
        string label,
        CancellationToken ct)
    {
        (int code, string body) = await client.GetHealthProbeAsync(path, ct);

        Console.WriteLine();
        Console.WriteLine($"{label} — HTTP {code}");
        Console.WriteLine(TruncateForDisplay(body, maxChars: 4000));

        return code >= 200 && code < 300;
    }

    private static string TruncateForDisplay(string body, int maxChars)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "(empty body)";
        }

        if (body.Length <= maxChars)
        {
            return body;
        }

        return body[..maxChars] + "\n... (truncated)";
    }
}
