using ArchLucid.Persistence.Connections;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="ReadReplicaRoutedConnectionFactory" /> for <see cref="ReadReplicaQueryRoute.AuthorityRunList" /> opens
///     via
///     <see cref="ResilientSqlConnectionFactory" /> when no replica string is set, then applies
///     <see cref="IRlsSessionContextApplicator" /> once.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AuthorityRunListConnectionFactoryTests
{
    [Fact]
    public async Task CreateOpenConnectionAsync_WithoutReplica_Uses_resilient_factory_and_applies_session_context()
    {
        Mock<ISqlConnectionFactory> inner = new();
        SqlConnection expected = new();
        inner.Setup(p => p.CreateOpenConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        ResilientSqlConnectionFactory resilient = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(maxRetryAttempts: 1));

        Mock<IOptionsMonitor<SqlServerOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new SqlServerOptions());

        Mock<IRlsSessionContextApplicator> applicator = new();
        applicator
            .Setup(a => a.ApplyAsync(expected, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReadReplicaRoutedConnectionFactory sut = new(
            resilient,
            options.Object,
            applicator.Object,
            ReadReplicaQueryRoute.AuthorityRunList);

        SqlConnection actual = await sut.CreateOpenConnectionAsync(CancellationToken.None);

        actual.Should().BeSameAs(expected);
        inner.Verify(p => p.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        applicator.Verify(a => a.ApplyAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }
}
