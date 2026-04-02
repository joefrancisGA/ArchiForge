using ArchiForge.Core.Pagination;

using FluentAssertions;

namespace ArchiForge.Core.Tests.Pagination;

[Trait("Category", "Unit")]
public sealed class PaginationDefaultsTests
{
    [Theory]
    [InlineData(0, 10, 1, 10)]
    [InlineData(-3, 10, 1, 10)]
    [InlineData(2, 0, 2, 1)]
    [InlineData(1, 500, 1, PaginationDefaults.MaxPageSize)]
    public void Normalize_ClampsPageAndPageSize(int page, int pageSize, int expectedPage, int expectedSize)
    {
        (int p, int s) = PaginationDefaults.Normalize(page, pageSize);

        p.Should().Be(expectedPage);
        s.Should().Be(expectedSize);
    }

    [Theory]
    [InlineData(1, 50, 0)]
    [InlineData(2, 10, 10)]
    [InlineData(3, 25, 50)]
    public void ToSkip_MatchesOneBasedPage(int page, int pageSize, int expectedSkip)
    {
        int skip = PaginationDefaults.ToSkip(page, pageSize);

        skip.Should().Be(expectedSkip);
    }
}
