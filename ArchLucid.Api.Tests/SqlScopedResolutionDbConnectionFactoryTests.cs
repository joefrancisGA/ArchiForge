using System.Data;

using ArchLucid.Host.Core.DataAccess;
using ArchLucid.Persistence.Connections;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="SqlScopedResolutionDbConnectionFactory" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SqlScopedResolutionDbConnectionFactoryTests
{
    [Fact]
    public async Task CreateOpenConnectionAsync_resolves_ISqlConnectionFactory_from_scope()
    {
        SqlConnection expected = new();
        Mock<ISqlConnectionFactory> sql = new();
        sql.Setup(s => s.CreateOpenConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        ServiceCollection services = [];
        services.AddScoped(_ => sql.Object);
        ServiceProvider provider = services.BuildServiceProvider();

        SqlScopedResolutionDbConnectionFactory sut = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True");

        IDbConnection conn = await sut.CreateOpenConnectionAsync(CancellationToken.None);

        conn.Should().BeSameAs(expected);
    }

    [Fact]
    public void CreateConnection_returns_unopened_SqlConnection()
    {
        ServiceCollection services = [];
        services.AddScoped<ISqlConnectionFactory>(_ => Mock.Of<ISqlConnectionFactory>());
        ServiceProvider provider = services.BuildServiceProvider();

        const string cs =
            "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
        SqlScopedResolutionDbConnectionFactory sut = new(provider.GetRequiredService<IServiceScopeFactory>(), cs);

        IDbConnection conn = sut.CreateConnection();

        conn.Should().BeOfType<SqlConnection>();
        conn.State.Should().Be(ConnectionState.Closed);
    }
}
