namespace ArchLucid.Api.Models.Admin;

/// <summary>JSON for <c>GET /v1/admin/roi-bulletin-preview</c> (aggregate baseline statistics only).</summary>
public sealed class RoiBulletinPreviewResponse
{
    public string Quarter
    {
        get; init;
    } = string.Empty;

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
