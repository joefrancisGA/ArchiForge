namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Roll-up KPIs included in the planning report (same semantics as <c>GET /v1/learning/summary</c>).</summary>
public sealed class LearningPlanningReportSummaryBlock
{
    public int ThemeCount { get; init; }

    public int PlanCount { get; init; }

    public int TotalThemeEvidenceSignals { get; init; }

    public int TotalLinkedSignalsAcrossPlans { get; init; }

    public int? MaxPlanPriorityScore { get; init; }
}
