using ArchLucid.Core.Scoping;

using Moq;

namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArtifactBlobTenantPathsTests
{
    private static IScopeContextProvider CreateProvider(Guid tenantId, Guid workspaceId, Guid projectId)
    {
        Mock<IScopeContextProvider> mock = new();
        mock.Setup(static m => m.GetCurrentScope()).Returns(
            new ScopeContext
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId
            });

        return mock.Object;
    }

    private static IScopeContextProvider CreateProvider(Guid tenantId)
    {
        return CreateProvider(tenantId, ScopeIds.DefaultWorkspace, ScopeIds.DefaultProject);
    }

    [SkippableFact]
    public void FormatArtifactContentRelativePath_formats_scope_and_artifact_segments()
    {
        Guid ws = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid proj = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid manifest = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid artifact = Guid.Parse("44444444-4444-4444-4444-444444444444");

        string path = ArtifactBlobTenantPaths.FormatArtifactContentRelativePath(ws, proj, manifest, artifact, "content.txt");

        path.Should().Be(
            "11111111-1111-1111-1111-111111111111/22222222-2222-2222-2222-222222222222/artifacts/"
            + "33333333-3333-3333-3333-333333333333/44444444-4444-4444-4444-444444444444/content.txt");
    }

    [SkippableFact]
    public void PrefixWithTenant_allows_logical_path_starting_with_workspace_guid()
    {
        Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid workspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        IScopeContextProvider provider = CreateProvider(tenantId, workspaceId, ScopeIds.DefaultProject);

        string logical = ArtifactBlobTenantPaths.FormatArtifactContentRelativePath(
            workspaceId,
            ScopeIds.DefaultProject,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "content.txt");

        string result = ArtifactBlobTenantPaths.PrefixWithTenant(provider, logical);
        result.Should().StartWith(tenantId.ToString("D") + "/");
        result.Should().Contain("/artifacts/");
    }

    [SkippableFact]
    public void FormatArtifactContentRelativePath_rejects_paths_in_file_name()
    {
        Action act = () => ArtifactBlobTenantPaths.FormatArtifactContentRelativePath(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "a/b.txt");

        act.Should().Throw<InvalidOperationException>();
    }

    [SkippableFact]
    public void PrefixWithTenant_prefixes_logical_path()
    {
        Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        IScopeContextProvider provider = CreateProvider(tenantId);
        string result = ArtifactBlobTenantPaths.PrefixWithTenant(provider, "exports/a.json");
        result.Should().Be(tenantId.ToString("D") + "/exports/a.json");
    }

    [SkippableFact]
    public void PrefixWithTenant_when_blob_already_has_tenant_prefix_throws()
    {
        Guid tenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        IScopeContextProvider provider = CreateProvider(tenantId);
        string prefix = tenantId.ToString("D") + "/";

        Action act = () => ArtifactBlobTenantPaths.PrefixWithTenant(provider, prefix + "x.json");
        act.Should().Throw<InvalidOperationException>();
    }

    [SkippableFact]
    public void PrefixWithTenant_when_first_segment_is_another_guid_still_prefixes_current_tenant()
    {
        Guid mine = Guid.Parse("30303030-3030-3030-3030-303030303030");
        Guid other = Guid.Parse("40404040-4040-4040-4040-404040404040");
        IScopeContextProvider provider = CreateProvider(mine);
        string path = other.ToString("D") + "/file.json";

        string result = ArtifactBlobTenantPaths.PrefixWithTenant(provider, path);

        result.Should().Be(mine.ToString("D") + "/" + path);
    }

    [SkippableFact]
    public void ThrowIfBlobRelativePathUnsafe_rejects_dot_dot()
    {
        Action act = () => ArtifactBlobTenantPaths.ThrowIfBlobRelativePathUnsafe("a/../b");
        act.Should().Throw<InvalidOperationException>();
    }

    [SkippableFact]
    public void EnsureReadBlobNameMatchesTenant_accepts_matching_prefix()
    {
        Guid tenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        IScopeContextProvider provider = CreateProvider(tenantId);
        string name = tenantId.ToString("D") + "/folder/file.json";

        Action act = () => ArtifactBlobTenantPaths.EnsureReadBlobNameMatchesTenant(provider, name);
        act.Should().NotThrow();
    }

    [SkippableFact]
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
