using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

[Trait("Category", "Unit")]
public sealed class SqlPagingSyntaxTests
{
    [Fact]
    public void FirstRowsOnly_ReturnsOffsetFetch()
    {
        string sql = SqlPagingSyntax.FirstRowsOnly(25);

        sql.Should().Be("OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FirstRowsOnly_Throws_WhenNotPositive(int rowCount)
    {
        Action act = () => SqlPagingSyntax.FirstRowsOnly(rowCount);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("rowCount");
    }
}
