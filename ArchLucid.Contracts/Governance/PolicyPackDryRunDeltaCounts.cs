namespace ArchLucid.Contracts.Governance;

/// <summary>
///     Aggregate "what-if" tally across every run id in the dry-run request body (not just the current page).
///     Used in the audit row's <c>deltaCounts</c> payload so reviewers see the headline impact at a glance.
/// </summary>
public sealed class PolicyPackDryRunDeltaCounts
{
    /// <summary>Total number of run ids the caller asked the service to evaluate (all pages combined).</summary>
    public int Evaluated
    {
        get;
        init;
    }

    /// <summary>How many runs the proposed thresholds would have blocked from commit.</summary>
    public int WouldBlock
    {
        get;
        init;
    }

    /// <summary>How many runs the proposed thresholds would have allowed.</summary>
    public int WouldAllow
    {
        get;
        init;
    }

    /// <summary>
    ///     How many run ids could not be loaded (run not found, scope mismatch, or read failure). These
    ///     are excluded from <see cref="WouldBlock" /> / <see cref="WouldAllow" />.
    /// </summary>
    public int RunMissing
    {
        get;
        init;
    }
}
