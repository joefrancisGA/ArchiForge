using ArchLucid.Application.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Tests for Baseline Mutation Audit Service.
/// </summary>

[Trait("Category", "Unit")]
public sealed class BaselineMutationAuditServiceTests
{
    [Fact]
    public async Task RecordAsync_WhenInformationEnabled_InvokesLoggerLog()
    {
        Mock<ILogger<BaselineMutationAuditService>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        BaselineMutationAuditService sut = new(logger.Object, Mock.Of<IAuditService>(), Mock.Of<IScopeContextProvider>());

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
        BaselineMutationAuditService sut = new(
            Mock.Of<ILogger<BaselineMutationAuditService>>(),
            Mock.Of<IAuditService>(),
            Mock.Of<IScopeContextProvider>());

        Func<Task> act = () => sut.RecordAsync("E", "  ", "id", null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RecordAsync_DetailsWithNewline_LogsSanitizedNoRawNewline()
    {
        Mock<ILogger<BaselineMutationAuditService>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        string? formatted = null;
        logger
            .Setup(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((IInvocation invocation) =>
            {
                object state = invocation.Arguments[2];
                Delegate? formatter = invocation.Arguments[4] as Delegate;

                if (formatter is not null)
                {
                    formatted = formatter.DynamicInvoke(state, null)?.ToString();
                }
            });

        BaselineMutationAuditService sut = new(logger.Object, Mock.Of<IAuditService>(), Mock.Of<IScopeContextProvider>());

        await sut.RecordAsync("Test.Event", "actor-1", "entity-1", "line1\nline2", CancellationToken.None);

        formatted.Should().NotBeNullOrEmpty();
        formatted.Should().Contain("line1_line2");
        formatted.Should().NotContain("\n");
    }
}
