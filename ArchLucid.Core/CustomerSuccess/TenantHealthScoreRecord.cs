namespace ArchLucid.Core.CustomerSuccess;

/// <summary>Materialized tenant health dimensions written by the scheduled scoring worker.</summary>
public sealed class TenantHealthScoreRecord
{
    public Guid TenantId
    {
        get;
        init;
    }

    public decimal EngagementScore
    {
        get;
        init;
    }

    public decimal BreadthScore
    {
        get;
        init;
    }

    public decimal QualityScore
    {
        get;
        init;
    }

    public decimal GovernanceScore
    {
        get;
        init;
    }

    public decimal SupportScore
    {
        get;
        init;
    }

    public decimal CompositeScore
    {
        get;
        init;
    }

    public DateTimeOffset UpdatedUtc
    {
        get;
        init;
    }
}
