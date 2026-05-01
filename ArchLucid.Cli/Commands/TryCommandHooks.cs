using ArchLucid.Cli.Real;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Injectable seam for <see cref="TryCommand.RunCoreAsync" />. Production binds these to real
///     implementations (Docker, HTTP, OS shell); unit tests bind in-memory fakes so the orchestrator
///     can be exercised hermetically.
/// </summary>
internal sealed class TryCommandHooks
{
    /// <summary>Locate the directory that contains <c>docker-compose.yml</c>; returns null when not found.</summary>
    public required Func<string?> FindComposeDirectory
    {
        get;
        init;
    }

    /// <summary>Brings up the pilot Docker stack and waits for <c>/health/ready</c>; returns a CLI exit code.</summary>
    public required Func<IReadOnlyList<string>, CancellationToken, Task<int>> PilotUp
    {
        get;
        init;
    }

    /// <summary>Validates <c>AZURE_OPENAI_*</c> host environment when real mode is active.</summary>
    public required Func<RealModePreflightResult> ValidateRealModeEnv
    {
        get;
        init;
    }

    /// <summary>Returns ordered overlay compose file names (relative) after <c>docker-compose.yml</c>.</summary>
    public required Func<bool, IReadOnlyList<string>> ResolveComposeOverlays
    {
        get;
        init;
    }

    /// <summary>POST /v1/demo/seed (best-effort).</summary>
    public required Func<string, CancellationToken, Task<TryCommand.DemoSeedOutcome>> DemoSeed
    {
        get;
        init;
    }

    /// <summary>Submit the sample architecture request via the API client.</summary>
    public required Func<ArchLucidApiClient, CancellationToken, Task<ArchLucidApiClient.CreateRunResult>> CreateRun
    {
        get;
        init;
    }

    /// <summary>POST /v1/architecture/run/{runId}/execute (best-effort).</summary>
    public required Func<string, string, bool, CancellationToken, Task<bool>> ExecuteRun
    {
        get;
        init;
    }

    /// <summary>GET /v1/architecture/run/{runId} (used by the polling loop).</summary>
    public required Func<ArchLucidApiClient, string, CancellationToken, Task<ArchLucidApiClient.GetRunResult?>> GetRun
    {
        get;
        init;
    }

    /// <summary>POST /v1/architecture/run/{runId}/seed (Development-only fallback).</summary>
    public required Func<ArchLucidApiClient, string, bool, CancellationToken,
            Task<ArchLucidApiClient.SeedFakeResultsResult?>>
        SeedFakeResults
    {
        get;
        init;
    }

    /// <summary>POST /v1/architecture/run/{runId}/commit.</summary>
    public required Func<ArchLucidApiClient, string, CancellationToken, Task<ArchLucidApiClient.CommitRunResult?>>
        CommitRun
    {
        get;
        init;
    }

    /// <summary>Save the first-value Markdown report to <c>savePath</c>; returns true on success.</summary>
    public required Func<string, string, string, CancellationToken, Task<bool>> DownloadFirstValueReport
    {
        get;
        init;
    }

    /// <summary>Open a local artifact (e.g. the saved Markdown) in the OS default handler.</summary>
    public required Action<string> OpenFile
    {
        get;
        init;
    }

    /// <summary>Open a URL in the default browser.</summary>
    public required Action<string> OpenUrl
    {
        get;
        init;
    }

    /// <summary>Construct the API client for the given base URL (factory so tests can stub it).</summary>
    public required Func<string, ArchLucidApiClient> CreateApiClient
    {
        get;
        init;
    }
}
