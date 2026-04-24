using ArchLucid.Core.Configuration;
using ArchLucid.Core.Notifications.Email;
using ArchLucid.Persistence.Notifications.Email;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests.Notifications.Email;

[Trait("Suite", "Persistence")]
[Trait("Category", "Unit")]
public sealed class AzureCommunicationServicesEmailProviderContractTests
{
    [Fact]
    public async Task SendAsync_invokes_acs_transport_with_html_and_plain_text_ordering()
    {
        Mock<IOptionsMonitor<EmailNotificationOptions>> options = new();
        options.Setup(static o => o.CurrentValue).Returns(
            new EmailNotificationOptions
            {
                Provider = EmailProviderNames.AzureCommunicationServices,
                AzureCommunicationServicesEndpoint = "https://contoso.communication.azure.com/",
                FromAddress = "DoNotReply@contoso.azurecomm.net"
            });

        Mock<IAzureCommunicationEmailApi> api = new();
        api.Setup(a => a.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("op-123");

        AzureCommunicationServicesEmailProvider sut = new(options.Object, api.Object);

        EmailMessage message = new()
        {
            To = "owner@example.com",
            Subject = "Hello",
            HtmlBody = "<p>Hi</p>",
            TextBody = "Hi",
            IdempotencyKey = "k",
            Tags = new EmailMessageTags { TenantId = Guid.NewGuid(), EventType = "t" }
        };

        await sut.SendAsync(message, CancellationToken.None);

        sut.ProviderName.Should().Be(EmailProviderNames.AzureCommunicationServices);

        api.Verify(
            a => a.SendAsync(
                "https://contoso.communication.azure.com/",
                null,
                "DoNotReply@contoso.azurecomm.net",
                "owner@example.com",
                "Hello",
                "Hi",
                "<p>Hi</p>",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
