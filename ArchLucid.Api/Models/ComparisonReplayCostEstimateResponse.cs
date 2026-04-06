namespace ArchiForge.Api.Models;

/// <summary>
/// API projection of <see cref="ArchiForge.Application.Analysis.ComparisonReplayCostEstimate"/>.
/// </summary>
public sealed class ComparisonReplayCostEstimateResponse
{
    public required string ComparisonRecordId
    {
        get; init;
    }
    public required string ComparisonType
    {
        get; init;
    }
    public required string Format
    {
        get; init;
    }
    public required string ReplayMode
    {
        get; init;
    }
    public bool PersistReplay
    {
        get; init;
    }
    public int ApproximateRelativeScore
    {
        get; init;
    }
    public required string RelativeCostBand
    {
        get; init;
    }
    public required IReadOnlyList<string> Factors
    {
        get; init;
    }

    public static ComparisonReplayCostEstimateResponse FromDomain(Application.Analysis.ComparisonReplayCostEstimate estimate)
    {
        ArgumentNullException.ThrowIfNull(estimate);

        return new ComparisonReplayCostEstimateResponse
        {
            ComparisonRecordId = estimate.ComparisonRecordId,
            ComparisonType = estimate.ComparisonType,
            Format = estimate.Format,
            ReplayMode = estimate.ReplayMode,
            PersistReplay = estimate.PersistReplay,
            ApproximateRelativeScore = estimate.ApproximateRelativeScore,
            RelativeCostBand = estimate.RelativeCostBand,
            Factors = estimate.Factors
        };
    }
}
