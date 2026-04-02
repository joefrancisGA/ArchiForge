using System.Reflection;

using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Delivery;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests.Alerts.Delivery;

[Trait("Category", "Unit")]
public sealed class AlertTeamsWebhookDeliveryChannelTests
{
    [Fact]
    public void ChannelType_ReturnsTeamsWebhook()
    {
        Mock<IWebhookPoster> poster = new();
        AlertTeamsWebhookDeliveryChannel sut = new(poster.Object);

        sut.ChannelType.Should().Be(AlertRoutingChannelType.TeamsWebhook);
    }

    [Fact]
    public async Task SendAsync_PostsJsonToSubscriptionDestination()
    {
        Mock<IWebhookPoster> poster = new();
        string expectedUrl = "https://outlook.office.com/webhook/test";

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Returns(Task.CompletedTask);

        AlertTeamsWebhookDeliveryChannel sut = new(poster.Object);

        await sut.SendAsync(CreatePayload(expectedUrl), CancellationToken.None);

        poster.Verify(
            x => x.PostJsonAsync(expectedUrl, It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_Body_HasTitleAndTextWithExpectedContent()
    {
        Mock<IWebhookPoster> poster = new();
        object? capturedBody = null;

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Callback<string, object, CancellationToken, WebhookPostOptions?>((_, body, _, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        AlertTeamsWebhookDeliveryChannel sut = new(poster.Object);
        AlertDeliveryPayload payload = CreatePayload("https://outlook.office.com/webhook/x");
        payload.Alert.Severity = "High";
        payload.Alert.Title = "Latency spike";
        payload.Alert.Category = "Performance";
        payload.Alert.TriggerValue = "p99 > 2s";
        payload.Alert.Description = "Check downstream service.";

        await sut.SendAsync(payload, CancellationToken.None);

        capturedBody.Should().NotBeNull();
        string title = GetStringProperty(capturedBody!, "title");
        string text = GetStringProperty(capturedBody!, "text");

        title.Should().Be("[High] Latency spike");
        text.Should().Contain("Category: Performance");
        text.Should().Contain("Trigger: p99 > 2s");
        text.Should().Contain("Check downstream service.");
    }

    [Fact]
    public async Task SendAsync_ForwardsCancellationToken()
    {
        Mock<IWebhookPoster> poster = new();
        CancellationToken expected = new(canceled: true);

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Returns(Task.CompletedTask);

        AlertTeamsWebhookDeliveryChannel sut = new(poster.Object);

        await sut.SendAsync(CreatePayload("https://outlook.office.com/webhook/x"), expected);

        poster.Verify(
            x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), expected, It.IsAny<WebhookPostOptions?>()),
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
                ChannelType = AlertRoutingChannelType.TeamsWebhook,
            },
        };
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        PropertyInfo? prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        prop.Should().NotBeNull($"property {propertyName} should exist on payload body");
        object? value = prop!.GetValue(target);

        value.Should().NotBeNull();

        return value!.ToString()!;
    }
}
