namespace ArchLucid.Api.Models.Analytics;

/// <summary>JSON for <c>GET /v1/analytics/roi</c> — tenant-level ROI aggregates (mocked until analytics persistence lands).</summary>
public sealed class ExecutiveRoiAggregatesResponse
{
    /// <summary>Engineering / review time avoided, in fractional hours (UI formats for display).</summary>
    public double TimeSavedHours { get; init; }

    public int DecisionsAutomated { get; init; }

    public int ComplianceRisksMitigated { get; init; }
}
