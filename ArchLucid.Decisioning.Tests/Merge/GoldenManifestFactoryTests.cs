using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Requests;
using ArchLucid.Decisioning.Merge;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests.Merge;

/// <summary>
/// Tests for <see cref="GoldenManifestFactory.CreateBase" /> mapping from <see cref="ArchitectureRequest" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class GoldenManifestFactoryTests
{
    [Fact]
    public void CreateBase_MapsRunId_SystemName_AndEmptyTopologyLists()
    {
        ArchitectureRequest request = SampleRequest();
        request.SystemName = "PaymentsApi";

        GoldenManifest manifest = GoldenManifestFactory.CreateBase(
            "run-alpha",
            request,
            "v3",
            parentManifestVersion: null);

        manifest.RunId.Should().Be("run-alpha");
        manifest.SystemName.Should().Be("PaymentsApi");
        manifest.Services.Should().BeEmpty();
        manifest.Datastores.Should().BeEmpty();
        manifest.Relationships.Should().BeEmpty();
    }

    [Fact]
    public void CreateBase_Metadata_MapsVersions_ChangeDescription_AndFreshUtcTimestamp()
    {
        ArchitectureRequest request = SampleRequest();
        DateTime before = DateTime.UtcNow;

        GoldenManifest manifest = GoldenManifestFactory.CreateBase(
            "run-42",
            request,
            manifestVersion: "v9",
            parentManifestVersion: "v8");

        DateTime after = DateTime.UtcNow;

        ManifestMetadata metadata = manifest.Metadata;
        metadata.ManifestVersion.Should().Be("v9");
        metadata.ParentManifestVersion.Should().Be("v8");
        metadata.ChangeDescription.Should().Be("Merged manifest for run run-42");
        metadata.DecisionTraceIds.Should().BeEmpty();
        metadata.CreatedUtc.Should().BeOnOrAfter(before);
        metadata.CreatedUtc.Should().BeOnOrBefore(after);
        metadata.CreatedUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void CreateBase_Metadata_ParentManifestVersion_Null_WhenNoParent()
    {
        ArchitectureRequest request = SampleRequest();

        GoldenManifest manifest = GoldenManifestFactory.CreateBase(
            "run-root",
            request,
            "v1",
            parentManifestVersion: null);

        manifest.Metadata.ParentManifestVersion.Should().BeNull();
    }

    [Fact]
    public void CreateBase_Metadata_ParentManifestVersion_PreservesEmptyString()
    {
        ArchitectureRequest request = SampleRequest();

        GoldenManifest manifest = GoldenManifestFactory.CreateBase(
            "run-empty-parent",
            request,
            "v2",
            parentManifestVersion: string.Empty);

        manifest.Metadata.ParentManifestVersion.Should().Be(string.Empty);
    }

    [Fact]
    public void CreateBase_Governance_AppliesDefaultClassificationsAndEmptyCollectionsExceptPolicyConstraints()
    {
        ArchitectureRequest request = SampleRequest();
        request.Constraints = ["must-use-private-endpoints", "no-public-smb"];

        GoldenManifest manifest = GoldenManifestFactory.CreateBase("run-g", request, "v1", null);

        ManifestGovernance governance = manifest.Governance;
        governance.RiskClassification.Should().Be("Moderate");
        governance.CostClassification.Should().Be("Moderate");
        governance.ComplianceTags.Should().BeEmpty();
        governance.RequiredControls.Should().BeEmpty();
        governance.PolicyConstraints.Should().Equal("must-use-private-endpoints", "no-public-smb");
    }

    [Fact]
    public void CreateBase_Governance_PolicyConstraints_IsCopy_NotSameInstanceAsRequest()
    {
        ArchitectureRequest request = SampleRequest();
        request.Constraints = ["a", "b"];

        GoldenManifest manifest = GoldenManifestFactory.CreateBase("run-copy", request, "v1", null);

        List<string> manifestConstraints = manifest.Governance.PolicyConstraints;
        manifestConstraints.Should().NotBeSameAs(request.Constraints);

        request.Constraints.Clear();
        manifestConstraints.Should().Equal("a", "b");
    }

    [Fact]
    public void CreateBase_Governance_PolicyConstraints_Empty_WhenRequestHasNoConstraints()
    {
        ArchitectureRequest request = SampleRequest();
        request.Constraints = [];

        GoldenManifest manifest = GoldenManifestFactory.CreateBase("run-empty", request, "v1", null);

        manifest.Governance.PolicyConstraints.Should().BeEmpty();
    }

    private static ArchitectureRequest SampleRequest()
    {
        return new ArchitectureRequest
        {
            Description = "Ten or more characters for a valid minimal description text sample.",
            SystemName = "DefaultSystem",
            Constraints = [],
        };
    }
}
