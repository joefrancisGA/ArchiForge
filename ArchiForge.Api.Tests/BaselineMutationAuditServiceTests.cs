using ArchiForge.Application;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class BaselineMutationAuditServiceTests
{
    [Fact]
    public async Task RecordAsync_WhenInformationEnabled_InvokesLoggerLog()
    {
        Mock<ILogger<BaselineMutationAuditService>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        BaselineMutationAuditService sut = new(logger.Object);

        await sut.RecordAsync("Test.Event", "actor-1", "entity-1", "short detail", CancellationToken.None);

        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("BaselineMutation", StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordAsync_EmptyActor_ThrowsArgumentException()
    {
        BaselineMutationAuditService sut = new(Mock.Of<ILogger<BaselineMutationAuditService>>());

        Func<Task> act = () => sut.RecordAsync("E", "  ", "id", null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
