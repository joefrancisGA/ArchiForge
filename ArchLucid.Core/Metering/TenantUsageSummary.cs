namespace ArchLucid.Core.Metering;

/// <summary>Aggregated usage for a tenant over a time window.</summary>
public sealed class TenantUsageSummary
{
    public Guid TenantId
    {
        get;
        init;
    }

    public UsageMeterKind Kind
    {
        get;
        init;
    }

    public long TotalQuantity
    {
        get;
        init;
    }

    public DateTimeOffset PeriodStartUtc
    {
        get;
        init;
    }

    public DateTimeOffset PeriodEndUtc
    {
        get;
        init;
    }
}
