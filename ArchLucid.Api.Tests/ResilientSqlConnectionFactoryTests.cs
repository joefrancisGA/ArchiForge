using ArchLucid.Persistence.Connections;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Verifies <see cref="ResilientSqlConnectionFactory" /> retry behaviour via <see cref="SqlOpenResilienceDefaults" />.
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

        ResilientSqlConnectionFactory sut = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(_logger, 3));

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

                return callCount == 1 ? throw new TimeoutException("transient") : Task.FromResult(expected);
            });

        ResilientSqlConnectionFactory sut = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(_logger, 3, TimeSpan.FromMilliseconds(1)));

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

        ResilientSqlConnectionFactory sut = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(_logger, 2, TimeSpan.FromMilliseconds(1)));

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

        ResilientSqlConnectionFactory sut = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(_logger, 3, TimeSpan.FromMilliseconds(1)));

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

        ResilientSqlConnectionFactory sut = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(_logger, 5, TimeSpan.FromSeconds(30)));

        Task<SqlConnection> task = sut.CreateOpenConnectionAsync(cts.Token);

        await Task.Delay(50, CancellationToken.None);
        await cts.CancelAsync();

        Func<Task> act = () => task;

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        Action act = () =>
        {
            _ = new ResilientSqlConnectionFactory(
                null!,
                SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(_logger));
        };

        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        Action act = () =>
        {
            _ = new ResilientSqlConnectionFactory(Mock.Of<ISqlConnectionFactory>(), null!);
        };

        act.Should().Throw<ArgumentNullException>().WithParameterName("sqlOpenRetryPipeline");
    }

    [Fact]
    public async Task ZeroRetries_FailsImmediatelyOnTransient()
    {
        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("transient"));

        ResilientSqlConnectionFactory sut = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(_logger, 0, TimeSpan.FromMilliseconds(1)));

        Func<Task> act = () => sut.CreateOpenConnectionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<TimeoutException>();

        inner.Verify(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
