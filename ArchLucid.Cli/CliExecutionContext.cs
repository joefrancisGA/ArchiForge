namespace ArchLucid.Cli;

/// <summary>
///     Per-invocation CLI flags. Reset by <see cref="Program.RunAsync" /> after each run.
/// </summary>
public static class CliExecutionContext
{
    /// <summary>
    ///     When true, the user passed one or more leading <c>--json</c> flags (e.g. <c>archlucid --json health</c>).
    ///     Subcommand-specific <c>--json</c> (e.g. after <c>comparisons list</c>) is unchanged.
    /// </summary>
    public static bool JsonOutput
    {
        get;
        internal set;
    }

    internal static string[] StripLeadingGlobalJsonFlags(string[] args, out bool json)
    {
        json = false;
        int i = 0;

        while (i < args.Length && string.Equals(args[i], "--json", StringComparison.Ordinal))
        {
            json = true;
            i++;
        }

        return i == 0 ? args : args[i..];
    }
}
