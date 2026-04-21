namespace ArchLucid.Cli.Commands;

/// <summary>Subset of <c>GET /v1/tenant/trial-status</c> consumed by the smoke runner.</summary>
internal sealed class TrialSmokeTrialStatusResponse
{
    public string Status { get; init; } = "None";
    public string? TrialWelcomeRunId { get; init; }
    public decimal? BaselineReviewCycleHours { get; init; }
    public string? FirstCommitUtc { get; init; }
}
