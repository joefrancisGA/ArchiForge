namespace ArchLucid.Contracts.Pilots;

/// <summary>Headline governance rollup for <see cref="SponsorEvidencePackResponse" /> (counts only).</summary>
public sealed class SponsorEvidenceGovernanceOutcomes
{
    /// <summary>Items in pending approval queues (bounded by dashboard query).</summary>
    public int PendingApprovalCount
    {
        get;
        init;
    }

    /// <summary>Recent terminal governance approvals (approved, rejected, promoted).</summary>
    public int RecentTerminalDecisionCount
    {
        get;
        init;
    }

    /// <summary>Tenant-scoped policy pack change-log rows.</summary>
    public int RecentPolicyPackChangeCount
    {
        get;
        init;
    }
}
