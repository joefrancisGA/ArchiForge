using System.Globalization;

namespace ArchLucid.Cli.Commands;

/// <summary>CLI arguments for <c>archlucid security-trust publish</c>.</summary>
internal sealed class SecurityTrustPublishCommandOptions
{
    public required string Kind
    {
        get;
        init;
    }

    public required string PublishedOn
    {
        get;
        init;
    }

    public required string SummaryUrl
    {
        get;
        init;
    }

    public string AssessorDisplayName
    {
        get;
        init;
    } = "Aeronova Red Team LLC";

    public string AssessmentCode
    {
        get;
        init;
    } = "2026-Q2";

    public string UiBaseUrl
    {
        get;
        init;
    } = TryCommandOptions.DefaultUiBaseUrl;

    public static SecurityTrustPublishCommandOptions? Parse(string[] args, out string? error)
    {
        error = null;

        if (args.Length == 0)
        {
            error =
                "Missing arguments. Usage: archlucid security-trust publish --kind pen-test --date <YYYY-MM-DD> "
                + "--summary-url <URL> [--assessor <name>] [--assessment-code <code>] [--ui-base-url <url>]";

            return null;
        }

        string kind = "pen-test";
        string? date = null;
        string? summaryUrl = null;
        string assessor = "Aeronova Red Team LLC";
        string assessmentCode = "2026-Q2";
        string uiBaseUrl = TryCommandOptions.DefaultUiBaseUrl;

        for (int i = 0; i < args.Length; i++)
        {
            string current = args[i];

            switch (current)
            {
                case "--kind":
                    if (i + 1 >= args.Length)
                    {
                        error = "--kind requires a value (only pen-test is supported).";

                        return null;
                    }

                    kind = args[++i].Trim();

                    break;

                case "--date":
                    if (i + 1 >= args.Length)
                    {
                        error = "--date requires YYYY-MM-DD.";

                        return null;
                    }

                    date = args[++i].Trim();

                    break;

                case "--summary-url":
                    if (i + 1 >= args.Length)
                    {
                        error = "--summary-url requires a URL or repo-relative path.";

                        return null;
                    }

                    summaryUrl = args[++i].Trim();

                    break;

                case "--assessor":
                    if (i + 1 >= args.Length)
                    {
                        error = "--assessor requires a display name.";

                        return null;
                    }

                    assessor = args[++i].Trim();

                    break;

                case "--assessment-code":
                    if (i + 1 >= args.Length)
                    {
                        error = "--assessment-code requires a value.";

                        return null;
                    }

                    assessmentCode = args[++i].Trim();

                    break;

                case "--ui-base-url":
                    if (i + 1 >= args.Length)
                    {
                        error = "--ui-base-url requires a base URL for the badge link hint.";

                        return null;
                    }

                    uiBaseUrl = args[++i].Trim();

                    break;

                default:
                    error = $"Unknown argument: {current}";

                    return null;
            }
        }

        if (!string.Equals(kind, "pen-test", StringComparison.OrdinalIgnoreCase))
        {
            error = "Only --kind pen-test is supported today.";

            return null;
        }

        if (string.IsNullOrWhiteSpace(date))
        {
            error = "--date (YYYY-MM-DD) is required.";

            return null;
        }

        if (!DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            error = "--date must be a valid calendar date (YYYY-MM-DD).";

            return null;
        }

        if (!string.IsNullOrWhiteSpace(summaryUrl))
            return new SecurityTrustPublishCommandOptions
            {
                Kind = kind,
                PublishedOn = date,
                SummaryUrl = summaryUrl,
                AssessorDisplayName = assessor,
                AssessmentCode = assessmentCode,
                UiBaseUrl = uiBaseUrl
            };

        error = "--summary-url is required.";

        return null;
    }
}
