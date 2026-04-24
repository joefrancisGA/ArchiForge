using ArchLucid.Core.Pagination;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Pagination;

[Trait("Category", "Unit")]
public sealed class PagedResponseBuilderTests
{
    [Fact]
    public void Build_ReturnsSlice_AndTotalCount()
    {
        IReadOnlyList<int> all = [1, 2, 3, 4, 5];

        PagedResponse<int> page = PagedResponseBuilder.Build(all, 2, 2);

        page.Items.Should().Equal(3, 4);
        page.TotalCount.Should().Be(5);
        page.Page.Should().Be(2);
        page.PageSize.Should().Be(2);
    }

    [Fact]
    public void FromDatabasePage_PreservesItems_AndNormalizesPaging()
    {
        IReadOnlyList<string> items = ["a"];

        PagedResponse<string> page = PagedResponseBuilder.FromDatabasePage(items, 99, 0, 5);

        page.Items.Should().Equal("a");
        page.TotalCount.Should().Be(99);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(5);
    }
}
