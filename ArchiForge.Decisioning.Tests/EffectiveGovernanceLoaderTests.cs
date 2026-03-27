using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Governance.Resolution;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests;

[Trait("Category", "Unit")]
public sealed class EffectiveGovernanceLoaderTests
{
    [Fact]
    public async Task LoadEffectiveContentAsync_ReturnsOnlyEffectiveContent_DiscardsDiagnostics()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        PolicyPackContentDocument effective = new()
        {
            ComplianceRuleKeys = ["rule-a"],
            AdvisoryDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["k"] = "v" }
        };

        Mock<IEffectiveGovernanceResolver> resolver = new();
        resolver
            .Setup(r => r.ResolveAsync(tenantId, workspaceId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new EffectiveGovernanceResolutionResult
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    EffectiveContent = effective,
                    Decisions = [new GovernanceResolutionDecision()],
                    Conflicts = [new GovernanceConflictRecord()],
                    Notes = ["summary"]
                });

        EffectiveGovernanceLoader loader = new(resolver.Object);

        PolicyPackContentDocument loaded = await loader.LoadEffectiveContentAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        loaded.Should().BeSameAs(effective);
        resolver.Verify(
            r => r.ResolveAsync(tenantId, workspaceId, projectId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadEffectiveContentAsync_PropagatesCancellation()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Mock<IEffectiveGovernanceResolver> resolver = new();
        resolver
            .Setup(r => r.ResolveAsync(tenantId, workspaceId, projectId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        EffectiveGovernanceLoader loader = new(resolver.Object);

        Func<Task> act = async () =>
            await loader.LoadEffectiveContentAsync(tenantId, workspaceId, projectId, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
