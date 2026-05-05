namespace ArchLucid.Core.Pilots;

/// <summary>Structured pilot closeout capture for sponsor proof-of-ROI (tenant-scoped SQL row).</summary>
public sealed class PilotCloseoutRecord
{
    public Guid CloseoutId
    {
        get;
        init;
    }

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

    public Guid? RunId
    {
        get;
        init;
    }

    public decimal? BaselineHours
    {
        get;
        init;
    }

    public byte SpeedScore
    {
        get;
        init;
    }

    public byte ManifestPackageScore
    {
        get;
        init;
    }

    public byte TraceabilityScore
    {
        get;
        init;
    }

    public string? Notes
    {
        get;
        init;
    }

    public DateTimeOffset CreatedUtc
    {
        get;
        init;
    }
}
