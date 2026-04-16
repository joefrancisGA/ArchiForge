using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Scoping;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Bootstrap;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ContosoRetailDemoIdsTests
{
    [Fact]
    public void ForTenant_default_tenant_matches_canonical_baseline_run()
    {
        ContosoRetailDemoIds ids = ContosoRetailDemoIds.ForTenant(ScopeIds.DefaultTenant);

        ids.AuthorityRunBaselineId.Should().Be(ContosoRetailDemoIdentifiers.AuthorityRunBaselineId);
        ids.RunBaseline.Should().Be(ContosoRetailDemoIdentifiers.RunBaseline);
        ids.RequestId.Should().Be(ContosoRetailDemoIdentifiers.RequestContoso);
    }

    [Fact]
    public void ForTenant_other_tenant_uses_distinct_baseline_run()
    {
        Guid other = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        ContosoRetailDemoIds ids = ContosoRetailDemoIds.ForTenant(other);

        ids.AuthorityRunBaselineId.Should().NotBe(ContosoRetailDemoIdentifiers.AuthorityRunBaselineId);
        ids.RequestId.Should().NotBe(ContosoRetailDemoIdentifiers.RequestContoso);
    }

    [Fact]
    public void ForTenant_is_stable_for_same_tenant()
    {
        Guid tenant = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        ContosoRetailDemoIds a = ContosoRetailDemoIds.ForTenant(tenant);
        ContosoRetailDemoIds b = ContosoRetailDemoIds.ForTenant(tenant);

        a.Should().Be(b);
    }
}
