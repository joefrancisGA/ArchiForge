namespace ArchLucid.Api.Models.Pilots;

/// <summary>JSON scorecard returned to sponsors and finance (MVP fields).</summary>
public sealed class PilotScorecardResponse
{
    public Guid TenantId
    {
        get; set;
    }

    public DateTimeOffset PeriodStart
    {
        get; set;
    }

    public DateTimeOffset PeriodEnd
    {
        get; set;
    }

    public int RunsInPeriod
    {
        get; set;
    }

    public int RunsWithCommittedManifest
    {
        get; set;
    }
}
