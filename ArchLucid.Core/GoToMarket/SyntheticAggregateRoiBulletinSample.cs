namespace ArchLucid.Core.GoToMarket;

/// <summary>
///     Deterministic aggregate-shaped statistics for <c>archlucid roi-bulletin --synthetic</c> and buyer-facing samples.
/// </summary>
/// <remarks>
///     <para>
///         The quarterly aggregate bulletin is normally sourced from <see cref="IRoiBulletinAggregateReader" />
///         (tenant-supplied
///         baseline hours). <see cref="ArchLucid.Application.Pilots.PilotRunDeltaComputer" /> instead computes
///         <em>per-run</em>
///         deltas (findings histogram, audit counts, LLM calls) for sponsor reports. This synthetic payload uses
///         <strong>
///             fixed
///             illustrative
///         </strong>
///         baseline-hour aggregates so procurement can preview bulletin shape before SQL-backed N≥5
///         samples exist; numbers are chosen to sit in the same order of magnitude as typical self-reported review-cycle
///         hours
///         documented alongside the Contoso retail demo narrative in <c>docs/PILOT_ROI_MODEL.md</c> — not read from
///         production.
///     </para>
/// </remarks>
public static class SyntheticAggregateRoiBulletinSample
{
    /// <summary>Minimum N shown on the synthetic bulletin (matches owner gate threshold for real issues).</summary>
    public const int SyntheticTenantCount = 5;

    /// <summary>Illustrative mean baseline hours (aggregate-only; not a customer measurement).</summary>
    public const decimal MeanBaselineHours = 22.4m;

    /// <summary>Illustrative p50 baseline hours.</summary>
    public const decimal MedianBaselineHours = 20m;

    /// <summary>Illustrative p90 baseline hours.</summary>
    public const decimal P90BaselineHours = 46m;

    /// <summary>Builds a sufficient-sample aggregate result for the requested quarter label.</summary>
    public static RoiBulletinAggregateReadResult ForQuarter(string quarterLabel)
    {
        return new RoiBulletinAggregateReadResult(
            true,
            SyntheticTenantCount,
            MeanBaselineHours,
            MedianBaselineHours,
            P90BaselineHours,
            quarterLabel);
    }
}
