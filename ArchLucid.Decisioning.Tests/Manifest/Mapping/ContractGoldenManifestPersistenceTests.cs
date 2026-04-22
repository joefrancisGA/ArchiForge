using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest.Mapping;

using FluentAssertions;

using Cm = ArchLucid.Contracts.Manifest;
using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Tests.Manifest.Mapping;

[Trait("Suite", "Core")]
public sealed class ContractGoldenManifestPersistenceTests
{
    [Fact]
    public void ResolveGoldenManifestForContractSave_when_authorityPersistBody_aligns_returns_same_instance_with_scope()
    {
        Guid manifestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid runId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid ctx = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid graph = Guid.Parse("44444444-4444-4444-4444-444444444444");
        Guid findings = Guid.Parse("55555555-5555-5555-5555-555555555555");
        Guid traceId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        };

        Dm.GoldenManifest body = new()
        {
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = ctx,
            GraphSnapshotId = graph,
            FindingsSnapshotId = findings,
            DecisionTraceId = traceId,
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
            CreatedUtc = DateTime.UtcNow,
        };

        SaveContractsManifestOptions keying = new()
        {
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = ctx,
            GraphSnapshotId = graph,
            FindingsSnapshotId = findings,
            DecisionTraceId = traceId,
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
        };

        Dm.GoldenManifest resolved = ContractGoldenManifestPersistence.ResolveGoldenManifestForContractSave(
            new Cm.GoldenManifest
            {
                RunId = runId.ToString("D"),
                SystemName = "x",
                Metadata = new Cm.ManifestMetadata(),
                Governance = new Cm.ManifestGovernance(),
            },
            scope,
            keying,
            body);

        resolved.Should().BeSameAs(body);
        resolved.TenantId.Should().Be(scope.TenantId);
        resolved.WorkspaceId.Should().Be(scope.WorkspaceId);
        resolved.ProjectId.Should().Be(scope.ProjectId);
    }

    [Fact]
    public void ResolveGoldenManifestForContractSave_when_manifest_id_mismatch_throws()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        SaveContractsManifestOptions keying = new()
        {
            ManifestId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            RuleSetId = "r",
            RuleSetVersion = "1",
            RuleSetHash = "h",
        };

        Dm.GoldenManifest body = new()
        {
            ManifestId = Guid.NewGuid(),
            RunId = keying.RunId,
            ContextSnapshotId = keying.ContextSnapshotId,
            GraphSnapshotId = keying.GraphSnapshotId,
            FindingsSnapshotId = keying.FindingsSnapshotId,
            DecisionTraceId = keying.DecisionTraceId,
            RuleSetId = keying.RuleSetId,
            RuleSetVersion = keying.RuleSetVersion,
            RuleSetHash = keying.RuleSetHash,
        };

        Action act = () => ContractGoldenManifestPersistence.ResolveGoldenManifestForContractSave(
            new Cm.GoldenManifest
            {
                RunId = body.RunId.ToString("D"),
                SystemName = "x",
                Metadata = new Cm.ManifestMetadata(),
                Governance = new Cm.ManifestGovernance(),
            },
            scope,
            keying,
            body);

        act.Should().Throw<ArgumentException>().WithParameterName("authorityPersistBody");
    }
}
