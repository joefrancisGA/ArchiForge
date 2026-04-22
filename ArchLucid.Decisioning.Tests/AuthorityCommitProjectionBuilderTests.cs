using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest;
using Cm = ArchLucid.Contracts.Manifest;
using Dm = ArchLucid.Decisioning.Models;

using FluentAssertions;

using Xunit;

namespace ArchLucid.Decisioning.Tests;

public sealed class AuthorityCommitProjectionBuilderTests
{
    [Fact]
    public async Task Build_marks_relationships_empty_and_maps_services_from_topology()
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
        };
        model.Metadata.Version = "v1";
        model.Metadata.Summary = "S";
        model.Metadata.Name = "N";
        model.Topology.Services.Add(
            new Cm.ManifestService
            {
                ServiceId = "a",
                ServiceName = "Api",
                ServiceType = ServiceType.Api,
                RuntimePlatform = RuntimePlatform.AppService,
            });

        Cm.GoldenManifest c = await sut.BuildAsync(
            model,
            new() { SystemName = "Contoso" },
            CancellationToken.None);
        c.Services.Should().HaveCount(1);
        c.Datastores.Should().BeEmpty();
        c.Relationships.Should().BeEmpty();
        c.RunId.Should().Be(model.RunId.ToString("D"));
        c.SystemName.Should().Be("Contoso");
    }
}
