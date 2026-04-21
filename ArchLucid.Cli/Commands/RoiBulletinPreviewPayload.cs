namespace ArchLucid.Cli.Commands;

/// <summary>JSON shape returned by <c>GET /v1/admin/roi-bulletin-preview</c>.</summary>
internal sealed class RoiBulletinPreviewPayload
{
    public string? Quarter
    {
        get; init;
    }

    public int TenantCount
    {
        get; init;
    }

    public decimal? MeanBaselineHours
    {
        get; init;
    }

    public decimal? MedianBaselineHours
    {
        get; init;
    }

    public decimal? P90BaselineHours
    {
        get; init;
    }
}
