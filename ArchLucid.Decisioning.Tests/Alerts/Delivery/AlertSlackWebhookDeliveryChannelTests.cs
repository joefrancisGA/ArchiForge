using System.Reflection;

using ArchLucid.Decisioning.Alerts;
using ArchLucid.Decisioning.Alerts.Delivery;
using ArchLucid.Notifications;

using FluentAssertions;

using Moq;

namespace ArchLucid.Decisioning.Tests.Alerts.Delivery;

[Trait("Category", "Unit")]
public sealed class AlertSlackWebhookDeliveryChannelTests
{
    [Fact]
    public void ChannelType_ReturnsSlackWebhook()
    {
        Mock<IWebhookPoster> poster = new();
        AlertSlackWebhookDeliveryChannel sut = new(poster.Object);

        sut.ChannelType.Should().Be(AlertRoutingChannelType.SlackWebhook);
    }

    [Fact]
    public async Task SendAsync_PostsJsonToSubscriptionDestination()
    {
        Mock<IWebhookPoster> poster = new();
        string expectedUrl = "https://hooks.slack.com/services/test";
        string? capturedUrl = null;

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Callback<string, object, CancellationToken, WebhookPostOptions?>((url, _, _, _) => capturedUrl = url)
            .Returns(Task.CompletedTask);

        AlertSlackWebhookDeliveryChannel sut = new(poster.Object);
        AlertDeliveryPayload payload = CreatePayload(expectedUrl);

        await sut.SendAsync(payload, CancellationToken.None);

        capturedUrl.Should().Be(expectedUrl);
        poster.Verify(
            x => x.PostJsonAsync(expectedUrl, It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_BodyText_IncludesSeverityTitleCategoryTriggerAndDescription()
    {
        Mock<IWebhookPoster> poster = new();
        object? capturedBody = null;

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Callback<string, object, CancellationToken, WebhookPostOptions?>((_, body, _, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        AlertSlackWebhookDeliveryChannel sut = new(poster.Object);
        AlertDeliveryPayload payload = CreatePayload("https://hooks.slack.com/x");
        payload.Alert.Severity = "Critical";
        payload.Alert.Title = "Disk full";
        payload.Alert.Category = "Infrastructure";
        payload.Alert.TriggerValue = "98%";
        payload.Alert.Description = "Expand volume or prune.";

        await sut.SendAsync(payload, CancellationToken.None);

        capturedBody.Should().NotBeNull();
        string text = GetStringProperty(capturedBody!, "text");

        text.Should().Contain("*[Critical]* Disk full");
        text.Should().Contain("Category: Infrastructure");
        text.Should().Contain("Trigger: 98%");
        text.Should().Contain("Expand volume or prune.");
    }

    [Fact]
    public async Task SendAsync_ForwardsCancellationToken()
    {
        Mock<IWebhookPoster> poster = new();
        CancellationToken expected = new(canceled: true);

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Returns(Task.CompletedTask);

        AlertSlackWebhookDeliveryChannel sut = new(poster.Object);

        await sut.SendAsync(CreatePayload("https://hooks.slack.com/x"), expected);

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
                Severity = "Warning",
                TriggerValue = "1",
                Description = "D",
            },
            Subscription = new AlertRoutingSubscription
            {
                Destination = destination,
                ChannelType = AlertRoutingChannelType.SlackWebhook,
            },
        };
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        PropertyInfo? prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        prop.Should().NotBeNull($"property {propertyName} should exist on payload body");
        object? value = prop.GetValue(target);

        value.Should().NotBeNull();

        return value.ToString()!;
    }
}
