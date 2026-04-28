using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

[Trait("Category", "Unit")]
public sealed class UnsupportedRelationalDbConnectionFactoryTests
{
    private readonly UnsupportedRelationalDbConnectionFactory _sut = new();

    [Fact]
    public void CreateConnection_Throws()
    {
        Action act = () => _sut.CreateConnection();

        act.Should().Throw<InvalidOperationException>().WithMessage("*InMemory*");
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_Throws()
    {
        Func<Task> act = async () => await _sut.CreateOpenConnectionAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*InMemory*");
    }
}
