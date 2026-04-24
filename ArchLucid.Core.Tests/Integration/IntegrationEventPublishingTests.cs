using ArchLucid.Core.Integration;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Core.Tests.Integration;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class IntegrationEventPublishingTests
{
    [Fact]
    public async Task TryPublishAsync_swallows_non_fatal_publish_failure()
    {
        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyDictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bus down"));

        Mock<ILogger> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        Func<Task> act = async () => await IntegrationEventPublishing.TryPublishAsync(
            publisher.Object,
            logger.Object,
            "com.archlucid.test",
            new { x = 1 },
            null,
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TryPublishAsync_does_not_catch_out_of_memory()
    {
        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyDictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OutOfMemoryException());

        Mock<ILogger> logger = new();

        Func<Task> act = async () => await IntegrationEventPublishing.TryPublishAsync(
            publisher.Object,
            logger.Object,
            "com.archlucid.test",
            new { x = 1 },
            null,
            CancellationToken.None);

        await act.Should().ThrowAsync<OutOfMemoryException>();
    }
}
