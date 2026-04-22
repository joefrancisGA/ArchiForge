namespace ArchLucid.Cli.Commands;

/// <summary>Parsed arguments for <see cref="SecondRunCommand"/>.</summary>
internal sealed class SecondRunCommandOptions
{
    public const string DefaultApiBaseUrl = TryCommandOptions.DefaultApiBaseUrl;

    public const string DefaultUiBaseUrl = TryCommandOptions.DefaultUiBaseUrl;

    public static readonly TimeSpan DefaultCommitDeadline = TryCommandOptions.DefaultCommitDeadline;

    public required string InputPath { get; init; }

    /// <summary>When true, <see cref="ApiBaseUrl"/> came from <c>--api-base-url</c> and must override <c>archlucid.json</c>.</summary>
    public bool ApiBaseUrlFromArgument { get; init; }

    public string ApiBaseUrl { get; init; } = DefaultApiBaseUrl;

    public string UiBaseUrl { get; init; } = DefaultUiBaseUrl;

    public bool OpenArtifacts { get; init; } = true;

    public TimeSpan CommitDeadline { get; init; } = DefaultCommitDeadline;

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Parses arguments after the <c>second-run</c> token. First positional is the input file path.</summary>
    public static SecondRunCommandOptions? Parse(string[] args, out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            error = "Usage: archlucid second-run <SECOND_RUN.toml|json> [--api-base-url <url>] [--ui-base-url <url>] [--no-open] [--commit-deadline <seconds>]";

            return null;
        }

        string inputPath = args[0];

        if (string.IsNullOrWhiteSpace(inputPath) || inputPath.StartsWith('-'))
        {
            error =
                "Missing input file. Usage: archlucid second-run <SECOND_RUN.toml|json> [--api-base-url <url>] [--ui-base-url <url>] [--no-open] [--commit-deadline <seconds>]";

            return null;
        }

        string apiBaseUrl = DefaultApiBaseUrl;
        bool apiBaseUrlFromArgument = false;
        string uiBaseUrl = DefaultUiBaseUrl;
        bool openArtifacts = true;
        TimeSpan commitDeadline = DefaultCommitDeadline;

        for (int i = 1; i < args.Length; i++)
        {
            string current = args[i];

            switch (current)
            {
                case "--no-open":
                    openArtifacts = false;
                    break;

                case "--api-base-url":
                    if (!TryReadNext(args, ref i, current, out string? apiValue, out error))
                        return null;

                    apiBaseUrl = apiValue!.TrimEnd('/');
                    apiBaseUrlFromArgument = true;
                    break;

                case "--ui-base-url":
                    if (!TryReadNext(args, ref i, current, out string? uiValue, out error))
                        return null;

                    uiBaseUrl = uiValue!.TrimEnd('/');
                    break;

                case "--commit-deadline":
                    if (!TryReadSeconds(args, ref i, current, out TimeSpan commitSpan, out error))
                        return null;

                    commitDeadline = commitSpan;
                    break;

                default:
                    error = $"Unknown argument for 'second-run': {current}.";

                    return null;
            }
        }

        error = null;

        return new SecondRunCommandOptions
        {
            InputPath = inputPath,
            ApiBaseUrlFromArgument = apiBaseUrlFromArgument,
            ApiBaseUrl = apiBaseUrl,
            UiBaseUrl = uiBaseUrl,
            OpenArtifacts = openArtifacts,
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
