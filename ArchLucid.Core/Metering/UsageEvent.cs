namespace ArchLucid.Core.Metering;

/// <summary>One usage metering row (persisted to <c>dbo.UsageEvents</c> when enabled).</summary>
public sealed class UsageEvent
{
    public Guid Id
    {
        get;
        init;
    } = Guid.NewGuid();

    public Guid TenantId
    {
        get;
        init;
    }

    public Guid WorkspaceId
    {
        get;
        init;
    }

    public Guid ProjectId
    {
        get;
        init;
    }

    public UsageMeterKind Kind
    {
        get;
        init;
    }

    public long Quantity
    {
        get;
        init;
    }

    public DateTimeOffset RecordedUtc
    {
        get;
        init;
    }

    public string? CorrelationId
    {
        get;
        init;
    }
}
