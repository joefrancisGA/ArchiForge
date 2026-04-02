using System.Reflection;

using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Scheduling;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests.Advisory.Delivery;

[Trait("Category", "Unit")]
public sealed class DigestTeamsWebhookDeliveryChannelTests
{
    [Fact]
    public void ChannelType_ReturnsTeamsWebhook()
    {
        Mock<IWebhookPoster> poster = new();
        DigestTeamsWebhookDeliveryChannel sut = new(poster.Object);

        sut.ChannelType.Should().Be(DigestDeliveryChannelType.TeamsWebhook);
    }

    [Fact]
    public async Task SendAsync_PostsJsonToSubscriptionDestination()
    {
        Mock<IWebhookPoster> poster = new();
        string expectedUrl = "https://outlook.office.com/webhook/digest";

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Returns(Task.CompletedTask);

        DigestTeamsWebhookDeliveryChannel sut = new(poster.Object);

        await sut.SendAsync(CreatePayload(expectedUrl), CancellationToken.None);

        poster.Verify(
            x => x.PostJsonAsync(expectedUrl, It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_Body_HasTitleAndTextWithSummaryAndMarkdown()
    {
        Mock<IWebhookPoster> poster = new();
        object? capturedBody = null;

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Callback<string, object, CancellationToken, WebhookPostOptions?>((_, body, _, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        DigestTeamsWebhookDeliveryChannel sut = new(poster.Object);
        DigestDeliveryPayload payload = CreatePayload("https://outlook.office.com/webhook/x");
        payload.Digest.Title = "Sprint digest";
        payload.Digest.Summary = "Coverage improved.";
        payload.Digest.ContentMarkdown = "### Changes\n- a";

        await sut.SendAsync(payload, CancellationToken.None);

        capturedBody.Should().NotBeNull();
        string title = GetStringProperty(capturedBody!, "title");
        string text = GetStringProperty(capturedBody!, "text");

        title.Should().Be("Sprint digest");
        text.Should().Be("Coverage improved.\n\n### Changes\n- a");
    }

    [Fact]
    public async Task SendAsync_WhenPayloadIsNull_ThrowsArgumentNullException()
    {
        Mock<IWebhookPoster> poster = new();
        DigestTeamsWebhookDeliveryChannel sut = new(poster.Object);

        Func<Task> act = async () => await sut.SendAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_ForwardsCancellationToken()
    {
        Mock<IWebhookPoster> poster = new();
        CancellationToken expected = new(canceled: true);

        poster
            .Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<WebhookPostOptions?>()))
            .Returns(Task.CompletedTask);

        DigestTeamsWebhookDeliveryChannel sut = new(poster.Object);

        await sut.SendAsync(CreatePayload("https://outlook.office.com/webhook/x"), expected);

        poster.Verify(
            x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<object>(), expected, It.IsAny<WebhookPostOptions?>()),
            Times.Once);
    }

    private static DigestDeliveryPayload CreatePayload(string destination)
    {
        return new DigestDeliveryPayload
        {
            Digest = new ArchitectureDigest
            {
                DigestId = Guid.NewGuid(),
                Title = "T",
                Summary = "S",
                ContentMarkdown = "M",
            },
            Subscription = new DigestSubscription
            {
                Destination = destination,
                ChannelType = DigestDeliveryChannelType.TeamsWebhook,
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
