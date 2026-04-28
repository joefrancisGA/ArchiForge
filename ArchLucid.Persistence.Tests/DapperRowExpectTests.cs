using ArchLucid.Persistence.Utilities;

namespace ArchLucid.Persistence.Tests;

public sealed class DapperRowExpectTests
{
    [Fact]
    public void Required_returns_row_when_not_null()
    {
        string row = DapperRowExpect.Required("a", "missing");
        Assert.Equal("a", row);
    }

    [Fact]
    public void Required_throws_when_null()
    {
        Assert.Throws<InvalidOperationException>(() => DapperRowExpect.Required<string>(null!, "missing"));
    }
}
