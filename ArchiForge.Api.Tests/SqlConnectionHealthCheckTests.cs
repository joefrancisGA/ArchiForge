using System.Data.Common;

using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Health;
using ArchiForge.Persistence.Data.Infrastructure;

using FluentAssertions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

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

        SqlConnectionHealthCheck sut = new(factory.Object, Options.Create(new ArchiForgeOptions { StorageProvider = "Sql" }));
        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Healthy_WhenInMemoryStorage_SkipsDatabaseOpen()
    {
        Mock<IDbConnectionFactory> factory = new();
        factory.Setup(f => f.CreateConnection()).Throws(new InvalidOperationException("should not open SQL"));

        SqlConnectionHealthCheck sut = new(factory.Object, Options.Create(new ArchiForgeOptions { StorageProvider = "InMemory" }));
        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description!.ToLowerInvariant().Should().Contain("inmemory");
    }

    [Fact]
    public async Task Degraded_WhenTimeoutExceptionThrown()
    {
        Mock<IDbConnectionFactory> factory = new();
        factory.Setup(f => f.CreateConnection())
            .Throws(new TimeoutException("Connection timed out"));

        SqlConnectionHealthCheck sut = new(factory.Object, Options.Create(new ArchiForgeOptions { StorageProvider = "Sql" }));
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

        SqlConnectionHealthCheck sut = new(factory.Object, Options.Create(new ArchiForgeOptions { StorageProvider = "Sql" }));
        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
    }
}
