using ArchLucid.Contracts.Marketing;

namespace ArchLucid.Application.Notifications.Email;

/// <summary>Notifies the sales inbox after an anonymous <c>/pricing</c> quote row is stored.</summary>
public interface IMarketingPricingQuoteSalesNotifier
{
    Task NotifyAsync(
        MarketingPricingQuoteRequestInsertResult insert,
        string workEmail,
        string companyName,
        string tierInterest,
        string message,
        CancellationToken cancellationToken);
}
