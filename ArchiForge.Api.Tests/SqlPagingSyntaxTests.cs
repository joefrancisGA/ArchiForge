using ArchiForge.Data.Infrastructure;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class SqlPagingSyntaxTests
{
    [Fact]
    public void FirstRowsOnly_UsesOffsetFetchSyntax()
    {
        string clause = SqlPagingSyntax.FirstRowsOnly(200);

        clause.Should().Be("OFFSET 0 ROWS FETCH NEXT 200 ROWS ONLY");
    }
}
