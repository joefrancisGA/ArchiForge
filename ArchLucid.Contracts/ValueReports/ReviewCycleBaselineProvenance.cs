namespace ArchLucid.Contracts.ValueReports;

/// <summary>
///     Whether the review-cycle baseline in <see cref="ValueReportSnapshot" /> came from the tenant row or ROI
///     defaults.
/// </summary>
public enum ReviewCycleBaselineProvenance
{
    TenantSuppliedAtSignup,

    /// <summary>When <c>dbo.Tenants.BaselineReviewCycleSource</c> is set to the reserved token <c>baseline_settings</c>.</summary>
    TenantSuppliedViaSettings,

    DefaultedFromRoiModelOptions,

    NoMeasurementYet
}
