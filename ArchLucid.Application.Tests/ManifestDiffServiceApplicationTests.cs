using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;

using FluentAssertions;

namespace ArchLucid.Application.Tests;

/// <summary>
/// Additional <see cref="ManifestDiffService"/> scenarios (relationships, cross-manifest warnings).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ManifestDiffServiceApplicationTests
{
    [SkippableFact]
    public void Compare_detects_added_and_removed_relationships()
    {
        GoldenManifest left = BaseManifest("v1");
        left.Relationships.Add(
            new ManifestRelationship { SourceId = "a", TargetId = "b", RelationshipType = RelationshipType.Calls, });

        GoldenManifest right = BaseManifest("v2");
        right.Relationships.Add(
            new ManifestRelationship { SourceId = "b", TargetId = "c", RelationshipType = RelationshipType.ReadsFrom, });

        ManifestDiffService sut = new();

        ManifestDiffResult diff = sut.Compare(left, right);

        diff.AddedRelationships.Should().ContainSingle(r => r.SourceId == "b" && r.TargetId == "c");
        diff.RemovedRelationships.Should().ContainSingle(r => r.SourceId == "a" && r.TargetId == "b");
    }

    [SkippableFact]
    public void Compare_emits_warnings_when_system_name_or_run_id_differ()
    {
        GoldenManifest left = BaseManifest("v1");
        left.SystemName = "SysA";
        left.RunId = "run-a";

        GoldenManifest right = BaseManifest("v2");
        right.SystemName = "SysB";
        right.RunId = "run-b";

        ManifestDiffService sut = new();

        ManifestDiffResult diff = sut.Compare(left, right);

        diff.Warnings.Should().Contain(w => w.Contains("SystemName", StringComparison.Ordinal));
        diff.Warnings.Should().Contain(w => w.Contains("RunId", StringComparison.Ordinal));
    }

    private static GoldenManifest BaseManifest(string version)
    {
        return new GoldenManifest
        {
            RunId = "run-1",
            SystemName = "S",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata { ManifestVersion = version },
        };
    }
}
