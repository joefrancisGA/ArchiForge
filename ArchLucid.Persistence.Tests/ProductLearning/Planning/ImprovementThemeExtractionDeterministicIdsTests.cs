using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Tests.ProductLearning.Planning;

[Trait("ChangeSet", "59R")]
public sealed class ImprovementThemeExtractionDeterministicIdsTests
{
    private static ProductLearningScope Scope() =>
        new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        };

    [SkippableFact]
    public void ThemeId_is_stable_for_same_scope_and_key()
    {
        ProductLearningScope scope = Scope();

        Guid a = ImprovementThemeExtractionDeterministicIds.ThemeId(scope, "pattern-x");
        Guid b = ImprovementThemeExtractionDeterministicIds.ThemeId(scope, "pattern-x");

        a.Should().Be(b);
    }

    [SkippableFact]
    public void ThemeId_changes_when_canonical_key_changes()
    {
        ProductLearningScope scope = Scope();

        Guid a = ImprovementThemeExtractionDeterministicIds.ThemeId(scope, "a");
        Guid b = ImprovementThemeExtractionDeterministicIds.ThemeId(scope, "b");

        a.Should().NotBe(b);
    }

    [SkippableFact]
    public void ThemeId_changes_when_tenant_changes()
    {
        ProductLearningScope scopeA = Scope();
        ProductLearningScope scopeB = new()
        {
            TenantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            WorkspaceId = scopeA.WorkspaceId,
            ProjectId = scopeA.ProjectId,
        };

        Guid a = ImprovementThemeExtractionDeterministicIds.ThemeId(scopeA, "k");
        Guid b = ImprovementThemeExtractionDeterministicIds.ThemeId(scopeB, "k");

        a.Should().NotBe(b);
    }

    [SkippableFact]
    public void EvidenceId_is_stable_for_same_inputs()
    {
        Guid themeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        Guid a = ImprovementThemeExtractionDeterministicIds.EvidenceId(themeId, "d1", 3);
        Guid b = ImprovementThemeExtractionDeterministicIds.EvidenceId(themeId, "d1", 3);

        a.Should().Be(b);
    }

    [SkippableFact]
    public void EvidenceId_differs_by_sequence_or_discriminator()
    {
        Guid themeId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        Guid a = ImprovementThemeExtractionDeterministicIds.EvidenceId(themeId, "d", 1);
        Guid b = ImprovementThemeExtractionDeterministicIds.EvidenceId(themeId, "d", 2);
        Guid c = ImprovementThemeExtractionDeterministicIds.EvidenceId(themeId, "other", 1);

        a.Should().NotBe(b);
        a.Should().NotBe(c);
    }
}
