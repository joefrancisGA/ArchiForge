namespace ArchLucid.Core.Tenancy;

/// <summary>
///     Result of the one-shot marker when the tenant&apos;s first committed golden manifest is recorded for trial
///     funnel latency.
/// </summary>
public sealed class TrialFirstManifestCommitOutcome
{
    /// <summary>Seconds from trial anchor (<c>TrialStartUtc</c> when set, otherwise <c>CreatedUtc</c>) to commit time.</summary>
    public double SignupToCommitSeconds
    {
        get;
        init;
    }

    /// <summary><c>TrialRunsUsed / TrialRunsLimit</c> after commit (0 when limit unset).</summary>
    public double TrialRunUsageRatio
    {
        get;
        init;
    }
}
