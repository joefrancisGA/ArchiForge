using System.Reflection;

using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Delivery;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests.Alerts.Delivery;

[Trait("Category", "Unit")]
public sealed class AlertOnCallWebhookDeliveryChannelTests
{
    [Fact]
    public void ChannelType_ReturnsOnCallWebhook()
    {
        Mock<IWebhookPoster> poster = new();
        AlertOnCallWebhookDeliveryChannel sut = new(poster.Object);

        sut.ChannelType.Should().Be(AlertRoutingChannelType.OnCallWebhook);
    }

    [Fact]
    public async Task SendAsync_PostsJsonToSubscriptionDestination()
    {
        Mock<IWebhookPoster> poster = new();
        string expectedUrl = "https://pager.example.com/ingest";

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Returns(Task.CompletedTask);

        AlertOnCallWebhookDeliveryChannel sut = new(poster.Object);

        await sut.SendAsync(CreatePayload(expectedUrl, runId: null), CancellationToken.None);

        poster.Verify(
            x => x.PostJsonAsync(expectedUrl, It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_Body_ContainsStructuredAlertFields()
    {
        Mock<IWebhookPoster> poster = new();
        object? capturedBody = null;
        Guid alertId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid runId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Callback<string, object, CancellationToken, WebhookPostOptions?>((_, body, _, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        AlertOnCallWebhookDeliveryChannel sut = new(poster.Object);
        AlertDeliveryPayload payload = CreatePayload("https://pager.example.com/x", runId);
        payload.Alert.AlertId = alertId;
        payload.Alert.Severity = "Critical";
        payload.Alert.Title = "Outage";
        payload.Alert.Category = "Availability";
        payload.Alert.TriggerValue = "errors > 10%";
        payload.Alert.Description = "Failover recommended.";
        payload.Alert.RunId = runId;

        await sut.SendAsync(payload, CancellationToken.None);

        capturedBody.Should().NotBeNull();

        GetStringProperty(capturedBody!, "severity").Should().Be("Critical");
        GetStringProperty(capturedBody!, "title").Should().Be("Outage");
        GetStringProperty(capturedBody!, "category").Should().Be("Availability");
        GetStringProperty(capturedBody!, "triggerValue").Should().Be("errors > 10%");
        GetStringProperty(capturedBody!, "description").Should().Be("Failover recommended.");
        GetPropertyValue(capturedBody!, "alertId").Should().Be(alertId);
        GetPropertyValue(capturedBody!, "runId").Should().Be(runId);
    }

    [Fact]
    public async Task SendAsync_Body_HasNullRunId_WhenAlertRunIdIsNull()
    {
        Mock<IWebhookPoster> poster = new();
        object? capturedBody = null;

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Callback<string, object, CancellationToken, WebhookPostOptions?>((_, body, _, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        AlertOnCallWebhookDeliveryChannel sut = new(poster.Object);
        AlertDeliveryPayload payload = CreatePayload("https://pager.example.com/x", runId: null);
        payload.Alert.RunId = null;

        await sut.SendAsync(payload, CancellationToken.None);

        capturedBody.Should().NotBeNull();
        GetPropertyValue(capturedBody!, "runId").Should().BeNull();
    }

    private static AlertDeliveryPayload CreatePayload(string destination, Guid? runId)
    {
        return new AlertDeliveryPayload
        {
            Alert = new AlertRecord
            {
                AlertId = Guid.NewGuid(),
                RunId = runId,
                Title = "T",
                Category = "C",
                Severity = "Warning",
                TriggerValue = "1",
                Description = "D",
            },
            Subscription = new AlertRoutingSubscription
            {
                Destination = destination,
                ChannelType = AlertRoutingChannelType.OnCallWebhook,
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

    private static object? GetPropertyValue(object target, string propertyName)
    {
        PropertyInfo? prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        prop.Should().NotBeNull($"property {propertyName} should exist on payload body");

        return prop!.GetValue(target);
    }
}
