using ArchLucid.Persistence.Connections;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="SessionContextSqlConnectionFactory" /> must not leak open connections when session context application
///     fails.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SessionContextSqlConnectionFactoryTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task CreateOpenConnectionAsync_when_applicator_throws_disposes_connection_and_rethrows()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        await using SqlConnection shared = new(fixture.ConnectionString);
        await shared.OpenAsync();

        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(shared);

        ResilientSqlConnectionFactory resilient = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(maxRetryAttempts: 0));

        Mock<IRlsSessionContextApplicator> applicator = new();
        applicator
            .Setup(a => a.ApplyAsync(shared, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("rls failed"));

        SessionContextSqlConnectionFactory sut = new(
            resilient,
            applicator.Object,
            NullLogger<SessionContextSqlConnectionFactory>.Instance);

        Func<Task> act = async () => await sut.CreateOpenConnectionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("rls failed*");

        Exception? afterDispose = await Record.ExceptionAsync(() => shared.OpenAsync());

        afterDispose.Should().NotBeNull("the inner connection must be disposed when RLS application fails");
        (afterDispose is ObjectDisposedException or InvalidOperationException).Should().BeTrue(
            "SqlClient may throw either type when OpenAsync is used on a disposed connection (platform-dependent)");
    }
}
