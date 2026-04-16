using ArchLucid.Application.Tenancy;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Tenancy;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TenantSlugNormalizerTests
{
    [Theory]
    [InlineData("Acme Corp", "acme-corp")]
    [InlineData("  SaaS-1  ", "saas-1")]
    [InlineData("a.b_c d", "a-b-c-d")]
    public void FromName_produces_expected_slug(string name, string expected)
    {
        string slug = TenantSlugNormalizer.FromName(name);

        slug.Should().Be(expected);
    }

    [Fact]
    public void FromName_throws_when_no_alphanumeric()
    {
        Action act = () => TenantSlugNormalizer.FromName("---");

        act.Should().Throw<InvalidOperationException>();
    }
}
