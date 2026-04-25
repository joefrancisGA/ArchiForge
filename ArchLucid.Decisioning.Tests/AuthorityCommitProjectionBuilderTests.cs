using ArchLucid.Contracts.Common;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest;

using FluentAssertions;

using Cm = ArchLucid.Contracts.Manifest;
using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Tests;

public sealed class AuthorityCommitProjectionBuilderTests
{
    [Fact]
    public async Task Build_projects_topology_services_datastores_and_relationships()
    {
        IAuthorityCommitProjectionBuilder sut = new AuthorityCommitProjectionBuilder();
        Dm.GoldenManifest model = new()
        {
            RunId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "x",
            RuleSetId = "r",
            RuleSetVersion = "1",
            RuleSetHash = "h",
            Metadata = { Version = "v1", Summary = "S", Name = "N" }
        };
        model.Topology.Services.Add(
            new Cm.ManifestService
            {
                ServiceId = "a",
                ServiceName = "Api",
                ServiceType = ServiceType.Api,
                RuntimePlatform = RuntimePlatform.AppService,
            });
        model.Topology.Relationships.Add(
            new Cm.ManifestRelationship
            {
                SourceId = "a",
                TargetId = "b",
                RelationshipType = RelationshipType.ReadsFrom
            });

        Cm.GoldenManifest c = await sut.BuildAsync(
            model,
            new()
            {
                SystemName = "Contoso"
            },
            CancellationToken.None);
        c.Services.Should().HaveCount(1);
        c.Datastores.Should().BeEmpty();
        c.Relationships.Should().HaveCount(1, "ADR 0030 PR A3 — TopologySection.Relationships now round-trips through the authority FK chain");
        c.Relationships[0].SourceId.Should().Be("a");
        c.Relationships[0].TargetId.Should().Be("b");
        c.RunId.Should().Be(model.RunId.ToString("N"), "ADR 0030 PR A3 — RunId projects as no-dashes (N) for API path consistency");
        c.SystemName.Should().Be("Contoso");
    }
}
