namespace ArchLucid.Application.Pilots;

/// <summary>Aggregated pilot metrics for a tenant scope over a UTC window (MVP — extend with governance counts later).</summary>
public sealed class PilotScorecardSummary
{
    public required Guid TenantId
    {
        get; init;
    }

    public required DateTimeOffset PeriodStart
    {
        get; init;
    }

    public required DateTimeOffset PeriodEnd
    {
        get; init;
    }

    public int RunsInPeriod
    {
        get; init;
    }

    public int RunsWithCommittedManifest
    {
        get; init;
    }
}
