namespace ArchLucid.Api.Models.Tenancy;

/// <summary>Optional body for <c>POST /v1/tenant/convert</c> (billing integration stub).</summary>
public sealed class TenantTrialConvertRequest
{
    /// <summary>Target commercial tier label for audit only (e.g. Team, Enterprise).</summary>
    public string? TargetTier { get; init; }
}
