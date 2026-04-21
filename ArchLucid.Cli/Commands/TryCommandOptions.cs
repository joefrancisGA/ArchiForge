namespace ArchLucid.Cli.Commands;

/// <summary>
/// Parsed arguments for <see cref="TryCommand"/>. All fields have defaults that match the demo overlay
/// (<c>docker-compose.demo.yml</c>): API on http://localhost:5000, operator UI on http://localhost:3000.
/// </summary>
internal sealed class TryCommandOptions
{
    public const string DefaultApiBaseUrl = "http://localhost:5000";
    public const string DefaultUiBaseUrl = "http://localhost:3000";
    public static readonly TimeSpan DefaultReadinessDeadline = TimeSpan.FromSeconds(120);
    public static readonly TimeSpan DefaultCommitDeadline = TimeSpan.FromSeconds(180);

    public string ApiBaseUrl { get; init; } = DefaultApiBaseUrl;

    public string UiBaseUrl { get; init; } = DefaultUiBaseUrl;

    /// <summary>When false, the command does not invoke the OS to open the saved Markdown file or browser URL.</summary>
    public bool OpenArtifacts { get; init; } = true;

    /// <summary>Maximum time to wait for <c>GET /health/ready</c> after <c>docker compose up</c>.</summary>
    public TimeSpan ReadinessDeadline { get; init; } = DefaultReadinessDeadline;

    /// <summary>Maximum time to wait for the sample run to reach <c>ReadyForCommit</c> (or fall back to seeding fake results).</summary>
    public TimeSpan CommitDeadline { get; init; } = DefaultCommitDeadline;

    /// <summary>Polling interval used while waiting for the sample run to become committable.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Parses CLI arguments after the leading <c>try</c> token. Returns null and sets <paramref name="error"/>
    /// when an argument is malformed (caller maps that to <see cref="CliExitCode.UsageError"/>).
    /// </summary>
    public static TryCommandOptions? Parse(string[] args, out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        string apiBaseUrl = DefaultApiBaseUrl;
        string uiBaseUrl = DefaultUiBaseUrl;
        bool openArtifacts = true;
        TimeSpan readinessDeadline = DefaultReadinessDeadline;
        TimeSpan commitDeadline = DefaultCommitDeadline;

        for (int i = 0; i < args.Length; i++)
        {
            string current = args[i];

            switch (current)
            {
                case "--no-open":
                    openArtifacts = false;
                    break;

                case "--api-base-url":
                    if (!TryReadNext(args, ref i, current, out string? apiValue, out error)) return null;
                    apiBaseUrl = apiValue!.TrimEnd('/');
                    break;

                case "--ui-base-url":
                    if (!TryReadNext(args, ref i, current, out string? uiValue, out error)) return null;
                    uiBaseUrl = uiValue!.TrimEnd('/');
                    break;

                case "--readiness-deadline":
                    if (!TryReadSeconds(args, ref i, current, out TimeSpan readinessSpan, out error)) return null;
                    readinessDeadline = readinessSpan;
                    break;

                case "--commit-deadline":
                    if (!TryReadSeconds(args, ref i, current, out TimeSpan commitSpan, out error)) return null;
                    commitDeadline = commitSpan;
                    break;

                default:
                    error = $"Unknown argument for 'try': {current}. Usage: archlucid try [--api-base-url <url>] [--ui-base-url <url>] [--no-open] [--readiness-deadline <seconds>] [--commit-deadline <seconds>]";
                    return null;
            }
        }

        error = null;

        return new TryCommandOptions
        {
            ApiBaseUrl = apiBaseUrl,
            UiBaseUrl = uiBaseUrl,
            OpenArtifacts = openArtifacts,
            ReadinessDeadline = readinessDeadline,
            CommitDeadline = commitDeadline,
        };
    }

    private static bool TryReadNext(string[] args, ref int index, string flag, out string? value, out string? error)
    {
        if (index + 1 >= args.Length)
        {
            value = null;
            error = $"Missing value for {flag}.";
            return false;
        }

        index++;
        value = args[index];
        error = null;
        return true;
    }

    private static bool TryReadSeconds(string[] args, ref int index, string flag, out TimeSpan span, out string? error)
    {
        if (!TryReadNext(args, ref index, flag, out string? raw, out error))
        {
            span = TimeSpan.Zero;
            return false;
        }

        if (!int.TryParse(raw, out int seconds) || seconds <= 0)
        {
            span = TimeSpan.Zero;
            error = $"Value for {flag} must be a positive integer (seconds).";
            return false;
        }

        span = TimeSpan.FromSeconds(seconds);
        error = null;
        return true;
    }
}
