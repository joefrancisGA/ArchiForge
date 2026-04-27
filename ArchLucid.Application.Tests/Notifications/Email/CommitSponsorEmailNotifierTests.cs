using ArchLucid.Application.Notifications.Email;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Notifications.Email;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Notifications.Email;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CommitSponsorEmailNotifierTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task NotifyAfterCommitAsync_when_admin_email_missing_does_not_send()
    {
        Mock<ITenantTrialEmailContactLookup> lookup = new();
        lookup
            .Setup(x => x.TryResolveAdminEmailAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        Mock<IEmailProvider> email = new();
        IOptionsMonitor<EmailNotificationOptions> options = BuildOptions(
            new EmailNotificationOptions { ProductDisplayName = "Prod", OperatorBaseUrl = "https://app.example" });

        CommitSponsorEmailNotifier sut = new(
            lookup.Object,
            email.Object,
            options,
            NullLogger<CommitSponsorEmailNotifier>.Instance);

        await sut.NotifyAfterCommitAsync(TenantId, "run-1", CancellationToken.None);

        email.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyAfterCommitAsync_when_admin_email_resolved_sends_with_run_link()
    {
        Mock<ITenantTrialEmailContactLookup> lookup = new();
        lookup
            .Setup(x => x.TryResolveAdminEmailAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("sponsor@example.com");

        Mock<IEmailProvider> email = new();
        email.Setup(x => x.ProviderName).Returns("test");

        IOptionsMonitor<EmailNotificationOptions> options = BuildOptions(
            new EmailNotificationOptions { ProductDisplayName = "Prod", OperatorBaseUrl = "https://app.example" });

        CommitSponsorEmailNotifier sut = new(
            lookup.Object,
            email.Object,
            options,
            NullLogger<CommitSponsorEmailNotifier>.Instance);

        await sut.NotifyAfterCommitAsync(TenantId, "run-abc", CancellationToken.None);

        email.Verify(
            x => x.SendAsync(
                It.Is<EmailMessage>(m =>
                    m.To == "sponsor@example.com"
                    && m.Subject.Contains("Prod", StringComparison.Ordinal)
                    && m.HtmlBody.Contains("run-abc", StringComparison.Ordinal)
                    && m.HtmlBody.Contains("https://app.example/runs/", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyAfterCommitAsync_when_send_fails_does_not_throw()
    {
        Mock<ITenantTrialEmailContactLookup> lookup = new();
        lookup
            .Setup(x => x.TryResolveAdminEmailAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("sponsor@example.com");

        Mock<IEmailProvider> email = new();
        email.Setup(x => x.ProviderName).Returns("test");
        email
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        IOptionsMonitor<EmailNotificationOptions> options = BuildOptions(new EmailNotificationOptions());

        CommitSponsorEmailNotifier sut = new(
            lookup.Object,
            email.Object,
            options,
            NullLogger<CommitSponsorEmailNotifier>.Instance);

        Func<Task> act = async () => await sut.NotifyAfterCommitAsync(TenantId, "run-z", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static IOptionsMonitor<EmailNotificationOptions> BuildOptions(EmailNotificationOptions value)
    {
        Mock<IOptionsMonitor<EmailNotificationOptions>> mock = new();
        mock.Setup(x => x.CurrentValue).Returns(value);

        return mock.Object;
    }
}
