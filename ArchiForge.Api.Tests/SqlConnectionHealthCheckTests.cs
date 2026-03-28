using System.Data;
using System.Data.Common;

using ArchiForge.Api.Health;
using ArchiForge.Data.Infrastructure;

using FluentAssertions;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Verifies <see cref="SqlConnectionHealthCheck"/> reports Healthy, Degraded, or Unhealthy
/// depending on the exception type thrown by <see cref="IDbConnectionFactory"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SqlConnectionHealthCheckTests
{
    [Fact]
    public async Task Healthy_WhenConnectionOpensSuccessfully()
    {
        Mock<DbConnection> mockConnection = new();
        mockConnection.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IDbConnectionFactory> factory = new();
        factory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);

        SqlConnectionHealthCheck sut = new(factory.Object);
        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Degraded_WhenTimeoutExceptionThrown()
    {
        Mock<IDbConnectionFactory> factory = new();
        factory.Setup(f => f.CreateConnection())
            .Throws(new TimeoutException("Connection timed out"));

        SqlConnectionHealthCheck sut = new(factory.Object);
        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("timed out");
    }

    [Fact]
    public async Task Unhealthy_WhenGenericExceptionThrown()
    {
        Mock<IDbConnectionFactory> factory = new();
        factory.Setup(f => f.CreateConnection())
            .Throws(new InvalidOperationException("Connection string missing"));

        SqlConnectionHealthCheck sut = new(factory.Object);
        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
    }
}
