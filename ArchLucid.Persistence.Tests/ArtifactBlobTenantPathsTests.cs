using ArchLucid.Core.Scoping;

using FluentAssertions;

using Moq;

namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArtifactBlobTenantPathsTests
{
    private static IScopeContextProvider CreateProvider(Guid tenantId)
    {
        Mock<IScopeContextProvider> mock = new();
        mock.Setup(static m => m.GetCurrentScope()).Returns(
            new ScopeContext
            {
                TenantId = tenantId, WorkspaceId = ScopeIds.DefaultWorkspace, ProjectId = ScopeIds.DefaultProject
            });

        return mock.Object;
    }

    [Fact]
    public void PrefixWithTenant_prefixes_logical_path()
    {
        Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        IScopeContextProvider provider = CreateProvider(tenantId);
        string result = ArtifactBlobTenantPaths.PrefixWithTenant(provider, "exports/a.json");
        result.Should().Be(tenantId.ToString("D") + "/exports/a.json");
    }

    [Fact]
    public void PrefixWithTenant_when_blob_already_has_tenant_prefix_throws()
    {
        Guid tenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        IScopeContextProvider provider = CreateProvider(tenantId);
        string prefix = tenantId.ToString("D") + "/";

        Action act = () => ArtifactBlobTenantPaths.PrefixWithTenant(provider, prefix + "x.json");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PrefixWithTenant_when_first_segment_is_another_tenant_guid_throws()
    {
        Guid mine = Guid.Parse("30303030-3030-3030-3030-303030303030");
        Guid other = Guid.Parse("40404040-4040-4040-4040-404040404040");
        IScopeContextProvider provider = CreateProvider(mine);
        string path = other.ToString("D") + "/file.json";

        Action act = () => ArtifactBlobTenantPaths.PrefixWithTenant(provider, path);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ThrowIfBlobRelativePathUnsafe_rejects_dot_dot()
    {
        Action act = () => ArtifactBlobTenantPaths.ThrowIfBlobRelativePathUnsafe("a/../b");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureReadBlobNameMatchesTenant_accepts_matching_prefix()
    {
        Guid tenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        IScopeContextProvider provider = CreateProvider(tenantId);
        string name = tenantId.ToString("D") + "/folder/file.json";

        Action act = () => ArtifactBlobTenantPaths.EnsureReadBlobNameMatchesTenant(provider, name);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureReadBlobNameMatchesTenant_rejects_other_tenant_prefix()
    {
        Guid mine = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        Guid other = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        IScopeContextProvider provider = CreateProvider(mine);
        string name = other.ToString("D") + "/x.json";

        Action act = () => ArtifactBlobTenantPaths.EnsureReadBlobNameMatchesTenant(provider, name);
        act.Should().Throw<InvalidOperationException>();
    }
}
