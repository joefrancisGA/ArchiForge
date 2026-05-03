namespace ArchLucid.Host.Core.Configuration;

/// <summary>Scheduled tenant health score materialization (leader-elected worker).</summary>
public sealed class TenantHealthScoringOptions
{
    public const string SectionName = "ArchLucid:TenantHealthScoring";

    /// <summary>When false, the hosted worker starts but performs no SQL work.</summary>
    public bool Enabled
    {
        get;
        init;
    } = true;

    /// <summary>Delay between full-tenant recomputation passes.</summary>
    public int IntervalHours
    {
        get;
        init;
    } = 24;
}
