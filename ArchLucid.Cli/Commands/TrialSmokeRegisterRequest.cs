namespace ArchLucid.Cli.Commands;

/// <summary>JSON payload sent by <see cref="TrialSmokeRunner"/> to <c>POST /v1/register</c>.</summary>
internal sealed class TrialSmokeRegisterRequest
{
    public string OrganizationName { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public string AdminDisplayName { get; init; } = string.Empty;
    public decimal? BaselineReviewCycleHours { get; init; }
    public string? BaselineReviewCycleSource { get; init; }
}
