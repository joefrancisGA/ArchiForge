using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Delivery;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests.Alerts.Delivery;

[Trait("Category", "Unit")]
public sealed class AlertEmailDeliveryChannelTests
{
    [Fact]
    public void ChannelType_ReturnsEmail()
    {
        Mock<IEmailSender> sender = new();
        AlertEmailDeliveryChannel sut = new(sender.Object);

        sut.ChannelType.Should().Be(AlertRoutingChannelType.Email);
    }

    [Fact]
    public async Task SendAsync_CallsEmailSenderWithSubscriptionDestination()
    {
        Mock<IEmailSender> sender = new();
        string expectedTo = "ops@example.com";

        sender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AlertEmailDeliveryChannel sut = new(sender.Object);

        await sut.SendAsync(CreatePayload(expectedTo), CancellationToken.None);

        sender.Verify(
            x => x.SendAsync(expectedTo, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_Subject_ContainsSeverityAndTitle()
    {
        Mock<IEmailSender> sender = new();
        string? capturedSubject = null;

        sender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, subject, _, _) => capturedSubject = subject)
            .Returns(Task.CompletedTask);

        AlertEmailDeliveryChannel sut = new(sender.Object);
        AlertDeliveryPayload payload = CreatePayload("a@b.c");
        payload.Alert.Severity = "Major";
        payload.Alert.Title = "Queue depth";

        await sut.SendAsync(payload, CancellationToken.None);

        capturedSubject.Should().Be("[Major] Queue depth");
    }

    [Fact]
    public async Task SendAsync_Body_ContainsCategorySeverityTriggerAndDescription()
    {
        Mock<IEmailSender> sender = new();
        string? capturedBody = null;

        sender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        AlertEmailDeliveryChannel sut = new(sender.Object);
        AlertDeliveryPayload payload = CreatePayload("a@b.c");
        payload.Alert.Category = "Messaging";
        payload.Alert.Severity = "Warning";
        payload.Alert.TriggerValue = "depth=5000";
        payload.Alert.Description = "Scale consumers.";

        await sut.SendAsync(payload, CancellationToken.None);

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("Category: Messaging");
        capturedBody.Should().Contain("Severity: Warning");
        capturedBody.Should().Contain("Trigger: depth=5000");
        capturedBody.Should().Contain("Scale consumers.");
    }

    [Fact]
    public async Task SendAsync_ForwardsCancellationToken()
    {
        Mock<IEmailSender> sender = new();
        CancellationToken expected = new(canceled: true);

        sender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AlertEmailDeliveryChannel sut = new(sender.Object);

        await sut.SendAsync(CreatePayload("a@b.c"), expected);

        sender.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), expected),
            Times.Once);
    }

    private static AlertDeliveryPayload CreatePayload(string destination)
    {
        return new AlertDeliveryPayload
        {
            Alert = new AlertRecord
            {
                AlertId = Guid.NewGuid(),
                Title = "T",
                Category = "C",
                Severity = "Info",
                TriggerValue = "1",
                Description = "D",
            },
            Subscription = new AlertRoutingSubscription
            {
                Destination = destination,
                ChannelType = AlertRoutingChannelType.Email,
            },
        };
    }
}
