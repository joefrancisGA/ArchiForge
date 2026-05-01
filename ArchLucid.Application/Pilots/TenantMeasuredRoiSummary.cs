using ArchLucid.Application.Billing;
using ArchLucid.Contracts.Pilots;

namespace ArchLucid.Application.Pilots;

/// <summary>
///     Operator-facing bundle: live process counters (same source as <see cref="IWhyArchLucidSnapshotService" />)
///     plus optional non-authoritative monthly spend band from <see cref="ITenantCostEstimateService" />.
/// </summary>
public sealed record TenantMeasuredRoiSummary(
    WhyArchLucidSnapshotResponse ProcessSignals,
    TenantCostEstimate? MonthlyCostBand,
    string Disclaimer);
