using ArchLucid.Application.Notifications.Email;
using ArchLucid.Contracts.Marketing;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Notifications.Email;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Notifications.Email;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class MarketingPricingQuoteSalesNotifierTests
{
    private static MarketingPricingQuoteRequestInsertResult SampleInsert =>
        new(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task NotifyAsync_when_email_provider_is_noop_does_not_send_and_does_not_throw()
    {
        Mock<IEmailProvider> email = new();
        email.SetupGet(x => x.ProviderName).Returns(EmailProviderNames.Noop);

        IOptionsMonitor<EmailNotificationOptions> options = BuildOptionsMonitor(
            new EmailNotificationOptions { PricingQuoteSalesInbox = "sales@archlucid.net" });

        MarketingPricingQuoteSalesNotifier sut = new(
            email.Object,
            options,
            NullLogger<MarketingPricingQuoteSalesNotifier>.Instance);

        await sut.NotifyAsync(
            SampleInsert,
            "buyer@example.com",
            "Contoso",
            "Team",
            "Hello",
            CancellationToken.None);

        email.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyAsync_when_inbox_empty_does_not_send()
    {
        Mock<IEmailProvider> email = new();
        email.SetupGet(x => x.ProviderName).Returns(EmailProviderNames.Smtp);

        IOptionsMonitor<EmailNotificationOptions> options = BuildOptionsMonitor(
            new EmailNotificationOptions { PricingQuoteSalesInbox = "  " });

        MarketingPricingQuoteSalesNotifier sut = new(
            email.Object,
            options,
            NullLogger<MarketingPricingQuoteSalesNotifier>.Instance);

        await sut.NotifyAsync(
            SampleInsert,
            "buyer@example.com",
            "Contoso",
            "Team",
            "Hello",
            CancellationToken.None);

        email.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyAsync_when_smtp_provider_sends_to_configured_inbox()
    {
        Mock<IEmailProvider> email = new();
        email.SetupGet(x => x.ProviderName).Returns(EmailProviderNames.Smtp);
        email
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        IOptionsMonitor<EmailNotificationOptions> options = BuildOptionsMonitor(
            new EmailNotificationOptions { PricingQuoteSalesInbox = "sales@archlucid.net" });

        MarketingPricingQuoteSalesNotifier sut = new(
            email.Object,
            options,
            NullLogger<MarketingPricingQuoteSalesNotifier>.Instance);

        await sut.NotifyAsync(
            SampleInsert,
            "buyer@example.com",
            "Contoso",
            "Team",
            "Hello",
            CancellationToken.None);

        email.Verify(
            x => x.SendAsync(
                It.Is<EmailMessage>(
                    m =>
                        m.To == "sales@archlucid.net" &&
                        m.Subject.Contains("pricing quote", StringComparison.OrdinalIgnoreCase) &&
                        m.IdempotencyKey.Contains("marketing-pricing-quote", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static IOptionsMonitor<EmailNotificationOptions> BuildOptionsMonitor(EmailNotificationOptions value)
    {
        Mock<IOptionsMonitor<EmailNotificationOptions>> mock = new();
        mock.Setup(x => x.CurrentValue).Returns(value);

        return mock.Object;
    }
}
