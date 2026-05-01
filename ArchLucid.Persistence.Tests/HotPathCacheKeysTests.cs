using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.Tests;

public sealed class HotPathCacheKeysTests
{
    [SkippableFact]
    public void Manifest_includes_scope_and_id()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        Guid manifestId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        string key = HotPathCacheKeys.Manifest(scope, manifestId);

        key.Should().Contain("11111111111111111111111111111111");
        key.Should().Contain("44444444444444444444444444444444");
        key.Should().StartWith("al:hot:hm:");
    }

    [SkippableFact]
    public void Run_uses_scope_project_id_column()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        };

        Guid runId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        string key = HotPathCacheKeys.Run(scope, runId);

        key.Should().StartWith("al:hot:run:");
        key.Should().Contain("cccccccccccccccccccccccccccccccc");
    }

    [SkippableFact]
    public void PolicyPack_key_is_stable()
    {
        Guid id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        HotPathCacheKeys.PolicyPack(id).Should().Be("al:hot:pp:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
    }
}
