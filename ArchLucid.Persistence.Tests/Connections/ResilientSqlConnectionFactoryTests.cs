using ArchLucid.Persistence.Connections;

using Microsoft.Data.SqlClient;

using Moq;

namespace ArchLucid.Persistence.Tests.Connections;

[Trait("Category", "Unit")]
public sealed class ResilientSqlConnectionFactoryTests
{
    [Fact]
    public async Task CreateOpenConnectionAsync_DelegatesToInnerThroughPipeline()
    {
        await using SqlConnection connection = new();
        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);

        ResilientSqlConnectionFactory sut = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(maxRetryAttempts: 0));

        SqlConnection result = await sut.CreateOpenConnectionAsync(CancellationToken.None);

        result.Should().BeSameAs(connection);
        inner.Verify(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_PropagatesInnerException()
    {
        Mock<ISqlConnectionFactory> inner = new();
        inner.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("open failed"));

        ResilientSqlConnectionFactory sut = new(
            inner.Object,
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(maxRetryAttempts: 0));

        Func<Task> act = async () => await sut.CreateOpenConnectionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("open failed");
    }
}
