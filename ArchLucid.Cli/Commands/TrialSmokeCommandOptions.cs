using System.Globalization;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Parsed arguments for <c>archlucid trial smoke</c>. Pure parsing (no I/O) so it is unit-testable.
/// </summary>
public sealed class TrialSmokeCommandOptions
{
    public const string DefaultDisplayName = "Trial Smoke User";

    /// <summary>
    ///     Canonical staging API host targeted by <c>--staging</c>. Must stay in sync with
    ///     <c>docs/runbooks/TRIAL_FUNNEL_END_TO_END.md</c> § 9 quick-start ("ARCHLUCID_API_URL=https://staging.archlucid.net").
    /// </summary>
    public const string StagingApiBaseUrl = "https://staging.archlucid.net";

    public string? ApiBaseUrl
    {
        get;
        init;
    }

    /// <summary>True when the caller passed <c>--staging</c> (auto-targets the staging API + one-line output).</summary>
    public bool TargetStaging
    {
        get;
        init;
    }

    /// <summary>True when the caller wants a single PASS|FAIL line (auto-on with <c>--staging</c>).</summary>
    public bool OneLineOutput
    {
        get;
        init;
    }

    public string OrganizationName
    {
        get;
        init;
    } = string.Empty;

    public string AdminEmail
    {
        get;
        init;
    } = string.Empty;

    public string AdminDisplayName
    {
        get;
        init;
    } = DefaultDisplayName;

    public decimal? BaselineReviewCycleHours
    {
        get;
        init;
    }

    public string? BaselineReviewCycleSource
    {
        get;
        init;
    }

    public bool SkipPilotRunDeltas
    {
        get;
        init;
    }

    /// <summary>
    ///     Parses the smoke-command tail after <c>trial smoke</c>. Returns null when invalid;
    ///     in that case <paramref name="error" /> contains a single-line user-friendly message.
    /// </summary>
    public static TrialSmokeCommandOptions? Parse(string[] args, out string? error)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        string? apiBaseUrl = null;
        string? org = null;
        string? email = null;
        string displayName = DefaultDisplayName;
        decimal? baselineHours = null;
        string? baselineSource = null;
        bool skipDeltas = false;
        bool targetStaging = false;
        bool oneLine = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--api-base-url":
                    if (!TryReadValue(args, ref i, arg, out string? apiVal, out error))
                        return null;
                    apiBaseUrl = apiVal;
                    break;

                case "--staging":
                    targetStaging = true;
                    break;

                case "--one-line":
                    oneLine = true;
                    break;

                case "--org":
                case "--organization":
                    if (!TryReadValue(args, ref i, arg, out string? orgVal, out error))
                        return null;
                    org = orgVal;
                    break;

                case "--email":
                    if (!TryReadValue(args, ref i, arg, out string? emailVal, out error))
                        return null;
                    email = emailVal;
                    break;

                case "--display-name":
                    if (!TryReadValue(args, ref i, arg, out string? dnVal, out error))
                        return null;
                    displayName = dnVal!;
                    break;

                case "--baseline-hours":
                    if (!TryReadValue(args, ref i, arg, out string? hoursVal, out error))
                        return null;
                    if (!decimal.TryParse(hoursVal, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal h) ||
                        h <= 0)
                    {
                        error = $"Invalid value for --baseline-hours: '{hoursVal}'. Expected a positive decimal.";
                        return null;
                    }

                    baselineHours = h;
                    break;

                case "--baseline-source":
                    if (!TryReadValue(args, ref i, arg, out string? srcVal, out error))
                        return null;
                    baselineSource = srcVal;
                    break;

                case "--skip-pilot-run-deltas":
                    skipDeltas = true;
                    break;

                default:
                    error = $"Unknown flag: {arg}. Try `archlucid trial smoke --help`.";
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(org))
        {
            error = "Missing --org. Provide an organization name for the smoke tenant.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Missing --email. Provide an administrator email for the smoke tenant.";
            return null;
        }

        if (baselineSource is not null && baselineHours is null)
        {
            error = "--baseline-source requires --baseline-hours.";
            return null;
        }

        if (targetStaging && !string.IsNullOrWhiteSpace(apiBaseUrl) &&
            !string.Equals(apiBaseUrl.Trim().TrimEnd('/'), StagingApiBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            error = $"--staging cannot be combined with a different --api-base-url ('{apiBaseUrl}'). Drop one.";
            return null;
        }

        if (targetStaging && string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            apiBaseUrl = StagingApiBaseUrl;
        }

        error = null;

        return new TrialSmokeCommandOptions
        {
            ApiBaseUrl = apiBaseUrl,
            TargetStaging = targetStaging,
            OneLineOutput = oneLine || targetStaging,
            OrganizationName = org.Trim(),
            AdminEmail = email.Trim(),
            AdminDisplayName = string.IsNullOrWhiteSpace(displayName) ? DefaultDisplayName : displayName.Trim(),
            BaselineReviewCycleHours = baselineHours,
            BaselineReviewCycleSource = baselineSource?.Trim(),
            SkipPilotRunDeltas = skipDeltas
        };
    }

    private static bool TryReadValue(string[] args, ref int i, string flag, out string? value, out string? error)
    {
        if (i + 1 >= args.Length)
        {
            value = null;
            error = $"Missing value for {flag}.";
            return false;
        }

        i++;
        value = args[i];
        error = null;
        return true;
    }
}
