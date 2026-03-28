using ArchiForge.Persistence.Connections;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Verifies <see cref="ResilientSqlConnectionFactory"/> retry and backoff behaviour.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ResilientSqlConnectionFactoryTests
{
    private readonly ILogger<ResilientSqlConnectionFactory> _logger =
        NullLogger<ResilientSqlConnectionFactory>.Instance;

    [Fact]
    public async Task Success_OnFirstAttempt_ReturnsConnection()
    {
        SqlConnection expected = new();
        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        ResilientSqlConnectionFactory sut = new(inner.Object, _logger);

        SqlConnection result = await sut.CreateOpenConnectionAsync(CancellationToken.None);

        result.Should().BeSameAs(expected);
        inner.Verify(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransientFailure_ThenSuccess_RetriesAndReturns()
    {
        SqlConnection expected = new();
        Mock<ISqlConnectionFactory> inner = new();

        int callCount = 0;
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(_ =>
            {
                callCount++;

                if (callCount == 1)
                    throw new TimeoutException("transient");

                return Task.FromResult(expected);
            });

        ResilientSqlConnectionFactory sut = new(inner.Object, _logger, maxRetries: 3, baseDelay: TimeSpan.FromMilliseconds(1));

        SqlConnection result = await sut.CreateOpenConnectionAsync(CancellationToken.None);

        result.Should().BeSameAs(expected);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task TransientFailure_ExhaustsRetries_ThrowsLastException()
    {
        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("persistent timeout"));

        ResilientSqlConnectionFactory sut = new(inner.Object, _logger, maxRetries: 2, baseDelay: TimeSpan.FromMilliseconds(1));

        Func<Task> act = () => sut.CreateOpenConnectionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<TimeoutException>().WithMessage("persistent timeout");

        // 1 initial + 2 retries = 3 total calls
        inner.Verify(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task NonTransientFailure_ThrowsImmediately()
    {
        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("config error"));

        ResilientSqlConnectionFactory sut = new(inner.Object, _logger, maxRetries: 3, baseDelay: TimeSpan.FromMilliseconds(1));

        Func<Task> act = () => sut.CreateOpenConnectionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("config error");

        inner.Verify(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancellation_DuringRetryDelay_PropagatesOperationCanceledException()
    {
        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("transient"));

        using CancellationTokenSource cts = new();

        // Use a longer delay so cancellation triggers during wait.
        ResilientSqlConnectionFactory sut = new(inner.Object, _logger, maxRetries: 5, baseDelay: TimeSpan.FromSeconds(30));

        Task<SqlConnection> task = sut.CreateOpenConnectionAsync(cts.Token);

        // Give the first attempt time to fail and enter the retry delay.
        await Task.Delay(50);
        cts.Cancel();

        Func<Task> act = () => task;

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ComputeDelay_ReturnsExponentiallyGrowingValues()
    {
        ResilientSqlConnectionFactory sut = new(
            Mock.Of<ISqlConnectionFactory>(),
            _logger,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(100));

        TimeSpan delay1 = sut.ComputeDelay(1);
        TimeSpan delay2 = sut.ComputeDelay(2);
        TimeSpan delay3 = sut.ComputeDelay(3);

        // With ±25 % jitter: attempt 1 ≈ 75..125 ms, attempt 2 ≈ 150..250 ms, attempt 3 ≈ 300..500 ms.
        delay1.TotalMilliseconds.Should().BeInRange(75, 125);
        delay2.TotalMilliseconds.Should().BeInRange(150, 250);
        delay3.TotalMilliseconds.Should().BeInRange(300, 500);
    }

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        Action act = () => new ResilientSqlConnectionFactory(null!, _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new ResilientSqlConnectionFactory(Mock.Of<ISqlConnectionFactory>(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ZeroRetries_FailsImmediatelyOnTransient()
    {
        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("transient"));

        ResilientSqlConnectionFactory sut = new(inner.Object, _logger, maxRetries: 0, baseDelay: TimeSpan.FromMilliseconds(1));

        Func<Task> act = () => sut.CreateOpenConnectionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<TimeoutException>();

        inner.Verify(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
