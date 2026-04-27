namespace ArchLucid.Contracts.Marketing;

/// <summary>Result of persisting an anonymous <c>/pricing</c> quote request (<c>dbo.MarketingPricingQuoteRequests</c>).</summary>
public readonly record struct MarketingPricingQuoteRequestInsertResult(Guid Id, DateTime CreatedUtc);
