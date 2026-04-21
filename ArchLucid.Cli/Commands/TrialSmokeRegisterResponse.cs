namespace ArchLucid.Cli.Commands;

/// <summary>JSON returned from <c>POST /v1/register</c>.</summary>
internal sealed class TrialSmokeRegisterResponse
{
    public string? TenantId { get; init; }
    public string? DefaultWorkspaceId { get; init; }
    public string? DefaultProjectId { get; init; }
}
