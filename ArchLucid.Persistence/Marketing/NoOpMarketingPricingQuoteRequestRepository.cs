namespace ArchLucid.Persistence.Marketing;

/// <summary>In-memory hosts: accept quote requests without SQL (drops silently).</summary>
public sealed class NoOpMarketingPricingQuoteRequestRepository : IMarketingPricingQuoteRequestRepository
{
    /// <inheritdoc />
    public Task AppendAsync(
        string workEmail,
        string companyName,
        string tierInterest,
        string message,
        byte[]? clientIpSha256,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
