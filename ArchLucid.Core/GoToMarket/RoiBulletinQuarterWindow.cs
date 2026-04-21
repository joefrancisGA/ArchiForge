namespace ArchLucid.Core.GoToMarket;

/// <summary>UTC window for filtering <c>BaselineReviewCycleCapturedUtc</c> when building aggregate ROI bulletins.</summary>
public sealed record RoiBulletinQuarterWindow(string Label, DateTimeOffset StartUtcInclusive, DateTimeOffset EndUtcExclusive);
