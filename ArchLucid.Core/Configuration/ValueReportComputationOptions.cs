namespace ArchLucid.Core.Configuration;

/// <summary>
///     Conservative ROI inputs aligned with <c>docs/go-to-market/ROI_MODEL.md</c> (hours, hourly rate, baseline annual
///     cost, LLM/run).
/// </summary>
public sealed class ValueReportComputationOptions
{
    public const string SectionPath = "ValueReport:Computation";

    /// <summary>Baseline architect effort per committed manifest before ArchLucid (hours).</summary>
    public decimal BaselineArchitectHoursBeforeArchLucidPerCommittedManifest
    {
        get;
        init;
    } = 8m;

    /// <summary>Fraction of baseline hours removed by automation (ROI doc §3.1 uses 50% for review-cycle reduction).</summary>
    public decimal ArchitectHoursSavedFractionVsBaseline
    {
        get;
        init;
    } = 0.5m;

    /// <summary>Person-hours attributed to each governance-class audit event in the window (conservative touch time).</summary>
    public decimal GovernanceReviewHoursPerGovernanceEvent
    {
        get;
        init;
    } = 0.5m;

    /// <summary>Person-hours attributed to each drift/alert-class audit event.</summary>
    public decimal DriftReviewHoursPerDriftAlertEvent
    {
        get;
        init;
    } = 0.25m;

    public decimal FullyLoadedArchitectHourlyUsd
    {
        get;
        init;
    } = 150m;

    /// <summary>Example all-in annual baseline from ROI_MODEL §4 (subscription + infra + LLM + setup amortized + ops).</summary>
    public decimal BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel
    {
        get;
        init;
    } = 27360m;

    public decimal EstimatedLlmUsdPerCompletedRun
    {
        get;
        init;
    } = 5m;

    /// <summary>
    ///     When <c>(to-from)</c> exceeds this many days, generation is queued and polled via
    ///     <c>GET …/jobs/{{jobId}}/docx</c>.
    /// </summary>
    public int AsyncJobWhenWindowDaysExceeds
    {
        get;
        init;
    } = 120;

    public string EstimatedLlmCostMethodologyNote
    {
        get;
        init;
    } =
        "LLM spend is estimated using docs/go-to-market/ROI_MODEL.md §4 (~USD per completed run). Token-level SQL accounting is not persisted in V1.";
}
