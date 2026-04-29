namespace ArchLucid.Contracts.Findings;

/// <summary>Human review state for AI-assisted or high-impact findings.</summary>
public enum FindingHumanReviewStatus
{
    NotRequired = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Overridden = 4
}
