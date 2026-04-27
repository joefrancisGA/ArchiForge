using System.Net;

using ArchLucid.Contracts.Marketing;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Notifications.Email;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Notifications.Email;

/// <inheritdoc cref="IMarketingPricingQuoteSalesNotifier" />
public sealed class MarketingPricingQuoteSalesNotifier(
    IEmailProvider emailProvider,
    IOptionsMonitor<EmailNotificationOptions> emailOptionsMonitor,
    ILogger<MarketingPricingQuoteSalesNotifier> logger) : IMarketingPricingQuoteSalesNotifier
{
    private const string EventType = "marketing-pricing-quote";

    private readonly IEmailProvider _emailProvider =
        emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));

    private readonly IOptionsMonitor<EmailNotificationOptions> _emailOptionsMonitor =
        emailOptionsMonitor ?? throw new ArgumentNullException(nameof(emailOptionsMonitor));

    private readonly ILogger<MarketingPricingQuoteSalesNotifier> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task NotifyAsync(
        MarketingPricingQuoteRequestInsertResult insert,
        string workEmail,
        string companyName,
        string tierInterest,
        string message,
        CancellationToken cancellationToken)
    {
        EmailNotificationOptions opts = _emailOptionsMonitor.CurrentValue;
        string? inbox = opts.PricingQuoteSalesInbox;

        if (string.IsNullOrWhiteSpace(inbox))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(
                    "Marketing pricing quote sales notification skipped: {ConfigKey} is empty.",
                    nameof(EmailNotificationOptions.PricingQuoteSalesInbox));

            return;
        }

        if (string.Equals(_emailProvider.ProviderName, EmailProviderNames.Noop, StringComparison.OrdinalIgnoreCase))
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation(
                    "Would notify pricing quote inbox {Inbox} for request id {RequestId} (Email:Provider is {Provider}).",
                    inbox.Trim(),
                    insert.Id,
                    _emailProvider.ProviderName);

            return;
        }

        string safeCompany = WebUtility.HtmlEncode(companyName);
        string safeTier = WebUtility.HtmlEncode(tierInterest);
        string safeEmail = WebUtility.HtmlEncode(workEmail);
        string safeMessage = WebUtility.HtmlEncode(message);
        string subject = "ArchLucid: new pricing quote request";

        string html =
            "<p>A new anonymous pricing quote was submitted from <strong>archlucid.net/pricing</strong>.</p>" +
            $"<p><strong>Request id:</strong> {insert.Id:N}</p>" +
            $"<p><strong>Created (UTC):</strong> {insert.CreatedUtc:O}</p>" +
            $"<p><strong>Work email:</strong> {safeEmail}</p>" +
            $"<p><strong>Company:</strong> {safeCompany}</p>" +
            $"<p><strong>Tier interest:</strong> {safeTier}</p>" +
            $"<p><strong>Message:</strong></p><pre style=\"white-space:pre-wrap\">{safeMessage}</pre>";

        string text =
            $"ArchLucid pricing quote request\nRequest id: {insert.Id:N}\nCreated (UTC): {insert.CreatedUtc:O}\n" +
            $"Work email: {workEmail}\nCompany: {companyName}\nTier: {tierInterest}\nMessage:\n{message}\n";

        EmailMessage email = new()
        {
            To = inbox.Trim(),
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
            IdempotencyKey = $"{EventType}:{insert.Id:N}",
            Tags = new EmailMessageTags
            {
                EventType = EventType,
            },
        };

        try
        {
            await _emailProvider.SendAsync(email, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(ex, "Marketing pricing quote sales email failed for request id {RequestId}.", insert.Id);
        }
    }
}
