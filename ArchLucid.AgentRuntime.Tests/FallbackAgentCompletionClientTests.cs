using System.Net;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>Unit tests for <see cref="FallbackAgentCompletionClient"/>.</summary>
[Trait("Category", "Unit")]
public sealed class FallbackAgentCompletionClientTests
{
    private static readonly LlmProviderDescriptor PrimaryDescriptor =
        LlmProviderDescriptor.ForAzureOpenAi(new Uri("https://primary.example"), "primary");

    [Fact]
    public async Task PrimarySucceeds_SecondaryNeverCalled()
    {
        Mock<IAgentCompletionClient> primary = new();
        Mock<IAgentCompletionClient> secondary = new();
        primary.Setup(p => p.Descriptor).Returns(PrimaryDescriptor);
        primary.Setup(p => p.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>())).ReturnsAsync("{}");

        FallbackAgentCompletionClient sut = new(
            primary.Object,
            secondary.Object,
            NullLogger<FallbackAgentCompletionClient>.Instance);

        string result = await sut.CompleteJsonAsync("s", "u");

        result.Should().Be("{}");
        secondary.Verify(
            s => s.CompleteJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PrimaryThrows429_SecondaryCalledAndResultReturned()
    {
        Mock<IAgentCompletionClient> primary = new();
        Mock<IAgentCompletionClient> secondary = new();
        primary.Setup(p => p.Descriptor).Returns(PrimaryDescriptor);
        primary.Setup(p => p.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("limit", null, HttpStatusCode.TooManyRequests));
        secondary.Setup(s => s.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"from\":\"secondary\"}");

        FallbackAgentCompletionClient sut = new(
            primary.Object,
            secondary.Object,
            NullLogger<FallbackAgentCompletionClient>.Instance);

        string result = await sut.CompleteJsonAsync("s", "u");

        result.Should().Be("{\"from\":\"secondary\"}");
        secondary.Verify(s => s.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PrimaryThrows500_SecondaryCalled()
    {
        Mock<IAgentCompletionClient> primary = new();
        Mock<IAgentCompletionClient> secondary = new();
        primary.Setup(p => p.Descriptor).Returns(PrimaryDescriptor);
        primary.Setup(p => p.CompleteJsonAsync("a", "b", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("srv", null, HttpStatusCode.InternalServerError));
        secondary.Setup(s => s.CompleteJsonAsync("a", "b", It.IsAny<CancellationToken>())).ReturnsAsync("{}");

        FallbackAgentCompletionClient sut = new(
            primary.Object,
            secondary.Object,
            NullLogger<FallbackAgentCompletionClient>.Instance);

        await sut.CompleteJsonAsync("a", "b");

        secondary.Verify(s => s.CompleteJsonAsync("a", "b", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PrimaryThrows400_ExceptionPropagated_SecondaryNotCalled()
    {
        Mock<IAgentCompletionClient> primary = new();
        Mock<IAgentCompletionClient> secondary = new();
        primary.Setup(p => p.Descriptor).Returns(PrimaryDescriptor);
        primary.Setup(p => p.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("bad", null, HttpStatusCode.BadRequest));

        FallbackAgentCompletionClient sut = new(
            primary.Object,
            secondary.Object,
            NullLogger<FallbackAgentCompletionClient>.Instance);

        Func<Task> act = async () => await sut.CompleteJsonAsync("s", "u");

        await act.Should().ThrowAsync<HttpRequestException>();
        secondary.Verify(
            s => s.CompleteJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PrimaryThrowsOperationCanceledException_NoFallback()
    {
        Mock<IAgentCompletionClient> primary = new();
        Mock<IAgentCompletionClient> secondary = new();
        primary.Setup(p => p.Descriptor).Returns(PrimaryDescriptor);
        primary.Setup(p => p.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("canceled"));

        FallbackAgentCompletionClient sut = new(
            primary.Object,
            secondary.Object,
            NullLogger<FallbackAgentCompletionClient>.Instance);

        Func<Task> act = async () => await sut.CompleteJsonAsync("s", "u");

        await act.Should().ThrowAsync<OperationCanceledException>();
        secondary.Verify(
            s => s.CompleteJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PrimaryAndSecondaryThrow_SecondaryExceptionPropagated()
    {
        Mock<IAgentCompletionClient> primary = new();
        Mock<IAgentCompletionClient> secondary = new();
        primary.Setup(p => p.Descriptor).Returns(PrimaryDescriptor);
        primary.Setup(p => p.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("limit", null, HttpStatusCode.TooManyRequests));
        InvalidOperationException secondaryFailure = new("secondary failed");
        secondary.Setup(s => s.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .ThrowsAsync(secondaryFailure);

        FallbackAgentCompletionClient sut = new(
            primary.Object,
            secondary.Object,
            NullLogger<FallbackAgentCompletionClient>.Instance);

        Func<Task> act = async () => await sut.CompleteJsonAsync("s", "u");

        Exception thrown = (await act.Should().ThrowAsync<Exception>()).Which;
        thrown.Should().BeSameAs(secondaryFailure);
    }
}
