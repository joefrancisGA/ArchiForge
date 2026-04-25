using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

using ArchLucid.Cli.Real;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     One-shot adoption-friction reducer: <c>archlucid try</c>. Brings up the pilot Docker stack, ensures the
///     demo seed exists, submits a sample architecture request, polls until commit, downloads the first-value
///     Markdown report, and opens the operator UI run-detail page. Composes existing commands — see
///     <see cref="PilotUpCommand" />, <see cref="ArchLucidApiClient" />, and <see cref="FirstValueReportCommand" />.
/// </summary>
internal static class TryCommand
{
    /// <summary>
    ///     Sample brief used when the user has not authored an <c>inputs/brief.md</c> in the current directory.
    ///     Intentionally generic so the simulator agents always succeed.
    /// </summary>
    internal const string SampleBrief =
        "Modernize a small retail web application running on Azure. The system needs scalable web tiers, " +
        "a SQL backend, and basic compliance with PCI DSS. Optimize for cost in a single region with " +
        "private endpoints for storage and a managed identity for the application.";

    internal const string SampleSystemName = "ArchLucidTryDemo";

    /// <summary>
    ///     Production entry point invoked from <see cref="Program" />. Wires real implementations into the
    ///     testable <see cref="RunCoreAsync" /> seam.
    /// </summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        TryCommandOptions? options = TryCommandOptions.Parse(args, out string? error);

        if (options is null)
        {
            Console.WriteLine(error);

            return CliExitCode.UsageError;
        }

        TryCommandHooks hooks = new()
        {
            FindComposeDirectory = PilotUpCommand.FindDockerComposeDirectory,
            PilotUp = PilotUpCommand.RunAsync,
            ValidateRealModeEnv = RealModePreflight.Validate,
            ResolveComposeOverlays = ComposeOverlayResolver.Resolve,
            DemoSeed = DemoSeedAsync,
            CreateRun = (api, ct) => api.CreateRunAsync(BuildSampleRequest(), ct),
            ExecuteRun = ExecuteRunAsync,
            GetRun = (api, runId, ct) => api.GetRunAsync(runId, ct),
            SeedFakeResults = (api, runId, fellback, ct) => api.SeedFakeResultsAsync(runId, fellback, ct),
            CommitRun = (api, runId, ct) => api.CommitRunAsync(runId, ct),
            DownloadFirstValueReport = (apiBaseUrl, runId, savePath, ct) =>
                FirstValueReportSaveAsync(apiBaseUrl, runId, savePath, ct),
            OpenFile = OpenLocalArtifact,
            OpenUrl = OpenInBrowser,
            CreateApiClient = baseUrl => new ArchLucidApiClient(baseUrl)
        };

        return await RunCoreAsync(options, hooks, Console.Out, cancellationToken);
    }

    /// <summary>
    ///     Testable orchestrator: takes an injectable <see cref="TryCommandHooks" /> bundle so unit tests can
    ///     drive the flow without invoking Docker, the network, or the OS shell.
    /// </summary>
    internal static async Task<int> RunCoreAsync(
        TryCommandOptions options,
        TryCommandHooks hooks,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hooks);
        ArgumentNullException.ThrowIfNull(output);

        string? composeDir = hooks.FindComposeDirectory();

        if (composeDir is null)
        {
            await output.WriteLineAsync(
                "Error: docker-compose.yml not found. Run 'archlucid try' from the ArchLucid repo root, or open the repo in the .devcontainer.");

            return CliExitCode.UsageError;
        }

        if (options.RealMode && !options.IsPilotRealAzureOpenAiAttempt)
        {
            await output.WriteLineAsync(
                $"Error: `--real` requires environment variable {TryCommandOptions.ArchLucidRealAoaiEnv}=1 (safety gate for the real Azure OpenAI compose overlay).");

            return CliExitCode.UsageError;
        }

        if (options.IsPilotRealAzureOpenAiAttempt)
        {
            RealModePreflightResult preflight = hooks.ValidateRealModeEnv();

            if (!preflight.IsOk)
            {
                await output.WriteLineAsync(preflight.ErrorMessage ?? "Real mode preflight failed.");

                return CliExitCode.UsageError;
            }
        }

        IReadOnlyList<string> composeAbsolutePaths =
            ComposePathListBuilder.BuildAbsolutePaths(composeDir, hooks.ResolveComposeOverlays(options.IsPilotRealAzureOpenAiAttempt));

        await output.WriteLineAsync(
            options.IsPilotRealAzureOpenAiAttempt
                ? "Step 1/5: Bringing up the pilot Docker stack (full-stack + demo + real Azure OpenAI overlay)..."
                : "Step 1/5: Bringing up the pilot Docker stack (full-stack + demo overlay)...");
        int pilotExit = await hooks.PilotUp(composeAbsolutePaths, cancellationToken);

        if (pilotExit != CliExitCode.Success)
        {
            await output.WriteLineAsync(
                $"Pilot stack failed to start (exit {pilotExit}). See output above for details.");

            return pilotExit;
        }

        await output.WriteLineAsync();
        await output.WriteLineAsync("Step 2/5: Ensuring Contoso demo seed (POST /v1/demo/seed)...");
        DemoSeedOutcome seedOutcome = await hooks.DemoSeed(options.ApiBaseUrl, cancellationToken);
        await output.WriteLineAsync(seedOutcome.Message);

        ArchLucidApiClient client = hooks.CreateApiClient(options.ApiBaseUrl);

        await output.WriteLineAsync();
        await output.WriteLineAsync("Step 3/5: Submitting a sample architecture request...");
        ArchLucidApiClient.CreateRunResult create = await hooks.CreateRun(client, cancellationToken);

        if (!create.Success || create.Response is null)
        {
            await output.WriteLineAsync($"Error: Could not create sample run. {create.Error}");

            return CliExitCode.OperationFailed;
        }

        string runId = create.Response.Run.RunId;
        await output.WriteLineAsync($"  RunId: {runId}");

        // Best-effort kick: simulator background service usually auto-dispatches, but calling /execute makes
        // the polling deterministic in restored stacks where the background loop has not yet swept new runs.
        bool executed = await hooks.ExecuteRun(
            options.ApiBaseUrl,
            runId,
            options.IsPilotRealAzureOpenAiAttempt,
            cancellationToken);

        if (!executed)
            await output.WriteLineAsync(
                "  (Note: explicit POST /execute did not succeed; will rely on simulator background loop.)");


        await output.WriteLineAsync();
        await output.WriteLineAsync(
            $"Step 4/5: Polling until run is ReadyForCommit (deadline {options.CommitDeadline.TotalSeconds:n0}s)...");
        ArchitectureRunStatus reached = await PollForCommittableStatusAsync(
            ct => GetStatusAsync(hooks.GetRun, client, runId, ct),
            options.CommitDeadline,
            options.PollInterval,
            cancellationToken);

        bool needsSeedFallback =
            reached == ArchitectureRunStatus.Failed || reached < ArchitectureRunStatus.ReadyForCommit;

        if (needsSeedFallback && options.StrictReal && options.IsPilotRealAzureOpenAiAttempt)
        {
            await output.WriteLineAsync(
                $"Error: `--strict-real` is set but the run did not reach ReadyForCommit (status: {reached}). " +
                "Fix Azure OpenAI connectivity/configuration or retry without `--strict-real` to allow simulator fallback.");

            return CliExitCode.OperationFailed;
        }

        if (needsSeedFallback)
        {
            await output.WriteLineAsync(
                $"  Run did not reach ReadyForCommit within {options.CommitDeadline.TotalSeconds:n0}s (status: {reached}). Falling back to seed-fake-results (development-only).");

            bool markPilotFallback = options.IsPilotRealAzureOpenAiAttempt;

            ArchLucidApiClient.SeedFakeResultsResult? seed =
                await hooks.SeedFakeResults(client, runId, markPilotFallback, cancellationToken);

            if (seed is null || !seed.Success)
            {
                await output.WriteLineAsync(
                    $"Error: Seed-fake-results fallback failed. {seed?.Error ?? "Unknown error."}");

                return CliExitCode.OperationFailed;
            }

            await output.WriteLineAsync($"  Seeded {seed.ResultCount} fake results.");
        }

        ArchLucidApiClient.CommitRunResult? commit = await hooks.CommitRun(client, runId, cancellationToken);

        if (commit is null || !commit.Success)
        {
            await output.WriteLineAsync($"Error: Commit failed. {commit?.Error ?? "Unknown error."}");

            return CliExitCode.OperationFailed;
        }

        string version = commit.Response?.Manifest.Metadata.ManifestVersion ?? "(unknown)";
        await output.WriteLineAsync($"  Committed. Manifest version: {version}");

        await output.WriteLineAsync();
        await output.WriteLineAsync("Step 5/5: Downloading the first-value report and opening artifacts...");

        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), $"first-value-{runId}.md");
        bool reportSaved =
            await hooks.DownloadFirstValueReport(options.ApiBaseUrl, runId, reportPath, cancellationToken);

        if (!reportSaved)
        {
            await output.WriteLineAsync(
                "  (Warning: first-value report download did not succeed; the run committed but the Markdown was not saved.)");
        }
        else
        {
            await output.WriteLineAsync($"  Wrote {reportPath}");

            if (options.OpenArtifacts)
                hooks.OpenFile(reportPath);
        }

        string runUrl = $"{options.UiBaseUrl.TrimEnd('/')}/runs/{Uri.EscapeDataString(runId)}";
        await output.WriteLineAsync($"  Operator UI: {runUrl}");

        if (options.OpenArtifacts)
            hooks.OpenUrl(runUrl);


        await output.WriteLineAsync();
        await output.WriteLineAsync(
            "Done. You have a committed manifest, a sponsor-grade Markdown report, and an open run.");

        return CliExitCode.Success;
    }

    /// <summary>
    ///     Polls the supplied status probe until the run is <see cref="ArchitectureRunStatus.ReadyForCommit" /> or
    ///     <see cref="ArchitectureRunStatus.Committed" />, the status is <see cref="ArchitectureRunStatus.Failed" />,
    ///     the <paramref name="deadline" /> elapses, or cancellation is requested.
    /// </summary>
    /// <remarks>
    ///     Pulled out as an internal static so unit tests can verify the timeout path with a deterministic
    ///     probe (no live API). The probe is allowed to return null when the run is not yet visible.
    /// </remarks>
    internal static async Task<ArchitectureRunStatus> PollForCommittableStatusAsync(
        Func<CancellationToken, Task<ArchitectureRunStatus?>> probe,
        TimeSpan deadline,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(probe);

        if (deadline <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(deadline));
        if (pollInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(pollInterval));

        // Stopwatch is monotonic and immune to wall-clock changes — important when this command runs inside
        // a freshly-booted devcontainer whose clock may step shortly after start.
        Stopwatch stopwatch = Stopwatch.StartNew();
        ArchitectureRunStatus last = ArchitectureRunStatus.Created;

        while (stopwatch.Elapsed < deadline && !cancellationToken.IsCancellationRequested)
        {
            ArchitectureRunStatus? observed = await probe(cancellationToken);

            if (observed.HasValue)
            {
                last = observed.Value;

                if (last == ArchitectureRunStatus.Failed)
                    return last;


                if (last == ArchitectureRunStatus.ReadyForCommit || last == ArchitectureRunStatus.Committed)
                    return last;
            }

            try
            {
                await Task.Delay(pollInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        return last;
    }

    private static async Task<ArchitectureRunStatus?> GetStatusAsync(
        Func<ArchLucidApiClient, string, CancellationToken, Task<ArchLucidApiClient.GetRunResult?>> getRun,
        ArchLucidApiClient client,
        string runId,
        CancellationToken cancellationToken)
    {
        ArchLucidApiClient.GetRunResult? detail = await getRun(client, runId, cancellationToken);

        if (detail is null)
            return null;


        return detail.Run.Status;
    }

    private static ArchitectureRequest BuildSampleRequest()
    {
        return new ArchitectureRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SystemName = SampleSystemName,
            Description = SampleBrief,
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["single-region", "low-cost"],
            RequiredCapabilities = ["web", "sql", "monitoring"],
            Assumptions = ["No existing infrastructure to reuse"],
            PriorManifestVersion = null
        };
    }

    /// <summary>
    ///     Best-effort POST to <c>/v1/demo/seed</c>. Tolerates 204 (success), 400 (Demo:Enabled false),
    ///     403 (insufficient authority outside Development), 404 (non-Development host), and connection failures.
    /// </summary>
    private static async Task<DemoSeedOutcome> DemoSeedAsync(string apiBaseUrl, CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(60) };
            http.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");

            string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

            if (!string.IsNullOrWhiteSpace(apiKey))
                http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);


            using HttpResponseMessage response = await http.PostAsync(
                "v1/demo/seed",
                new StringContent(string.Empty, Encoding.UTF8, "application/json"),
                cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.NoContent => new DemoSeedOutcome(true, "  Demo seed OK (or no-op when already seeded)."),
                HttpStatusCode.BadRequest => new DemoSeedOutcome(false,
                    "  Demo seed disabled (Demo:Enabled=false). Continuing — startup auto-seed may have already run."),
                HttpStatusCode.Forbidden => new DemoSeedOutcome(false,
                    "  Demo seed forbidden (insufficient authority). Continuing — startup auto-seed may have already run."),
                HttpStatusCode.NotFound => new DemoSeedOutcome(false,
                    "  Demo seed unavailable (host is not Development). Continuing."),
                _ => new DemoSeedOutcome(false, $"  Demo seed returned HTTP {(int)response.StatusCode}. Continuing.")
            };
        }
        catch (Exception ex)
        {
            return new DemoSeedOutcome(false,
                $"  Demo seed call failed: {ex.GetType().Name}: {ex.Message}. Continuing.");
        }
    }

    /// <summary>
    ///     Best-effort POST <c>/v1/architecture/run/{runId}/execute</c> to dispatch agents. Returns true on
    ///     2xx; false on any failure (the caller falls back to polling for the simulator background loop).
    /// </summary>
    private static async Task<bool> ExecuteRunAsync(
        string apiBaseUrl,
        string runId,
        bool pilotTryRealMode,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };
            http.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

            if (!string.IsNullOrWhiteSpace(apiKey))
                http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);


            if (pilotTryRealMode)
                http.DefaultRequestHeaders.Add(PilotTryRealModeHeaders.PilotTryRealMode, "1");


            using HttpResponseMessage response = await http.PostAsync(
                $"v1/architecture/run/{Uri.EscapeDataString(runId)}/execute",
                new StringContent(string.Empty, Encoding.UTF8, "application/json"),
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Best-effort — the simulator may auto-dispatch; we'll know from the next status poll.
            return false;
        }
    }

    private static async Task<bool> FirstValueReportSaveAsync(
        string apiBaseUrl,
        string runId,
        string savePath,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(60) };
            http.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/markdown"));

            string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

            if (!string.IsNullOrWhiteSpace(apiKey))
                http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);


            using HttpResponseMessage response = await http.GetAsync(
                $"v1/pilots/runs/{Uri.EscapeDataString(runId)}/first-value-report",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;


            string markdown = await response.Content.ReadAsStringAsync(cancellationToken);
            await File.WriteAllTextAsync(savePath, markdown, cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void OpenLocalArtifact(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch
        {
            // Best-effort — the path was already printed.
        }
    }

    private static void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // Best-effort — the URL was already printed.
        }
    }

    /// <summary>Outcome record for the best-effort demo-seed call (kept simple — never fatal).</summary>
    internal sealed record DemoSeedOutcome(bool Success, string Message);
}
