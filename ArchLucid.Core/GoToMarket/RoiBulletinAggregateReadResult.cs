namespace ArchLucid.Core.GoToMarket;

/// <summary>Outcome of <see cref="IRoiBulletinAggregateReader.ReadAsync" /> for admin bulletin preview.</summary>
public sealed record RoiBulletinAggregateReadResult(
    bool IsSufficientSample,
    int TenantCount,
    decimal? MeanBaselineHours,
    decimal? MedianBaselineHours,
    decimal? P90BaselineHours,
    string QuarterLabel);
