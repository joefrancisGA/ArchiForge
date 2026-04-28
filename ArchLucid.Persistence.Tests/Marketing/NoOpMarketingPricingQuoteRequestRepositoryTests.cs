using ArchLucid.Contracts.Marketing;
using ArchLucid.Persistence.Marketing;

namespace ArchLucid.Persistence.Tests.Marketing;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class NoOpMarketingPricingQuoteRequestRepositoryTests
{
    [Fact]
    public async Task AppendAsync_returns_null_and_does_not_throw()
    {
        NoOpMarketingPricingQuoteRequestRepository sut = new();

        MarketingPricingQuoteRequestInsertResult? r = await sut.AppendAsync(
            "a@b.com",
            "Co",
            "Team",
            "Hi",
            null,
            CancellationToken.None);

        r.Should().BeNull();
    }
}
