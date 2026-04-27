using ArchLucid.Contracts.Marketing;

namespace ArchLucid.Persistence.Marketing;

/// <summary>Append-only persistence for anonymous pricing quote requests.</summary>
public interface IMarketingPricingQuoteRequestRepository
{
    /// <summary>Returns <see langword="null" /> when storage is NoOp (in-memory host).</summary>
    Task<MarketingPricingQuoteRequestInsertResult?> AppendAsync(
        string workEmail,
        string companyName,
        string tierInterest,
        string message,
        byte[]? clientIpSha256,
        CancellationToken cancellationToken);
}
