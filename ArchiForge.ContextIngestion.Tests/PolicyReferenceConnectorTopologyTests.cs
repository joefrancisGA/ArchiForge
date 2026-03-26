using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Models;

using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

[Trait("Category", "Unit")]
public sealed class PolicyReferenceConnectorTopologyTests
{
    [Fact]
    public async Task NormalizeAsync_WhenPolicyOverlapsTopologyHint_SetsApplicableTopologyNodeIds()
    {
        PolicyReferenceConnector sut = new();
        RawContextPayload raw = new()
        {
            PolicyReferences = ["prod-vnet-policy"],
            TopologyHints = ["prod-vnet-policy-subnet"]
        };

        NormalizedContextBatch batch = await sut.NormalizeAsync(raw, CancellationToken.None);

        CanonicalObject policy = batch.CanonicalObjects.Single();
        policy.Properties.Should().ContainKey("applicableTopologyNodeIds");
        string ids = policy.Properties["applicableTopologyNodeIds"];
        ids.Should().StartWith("obj-");
        ids.Split(',', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(1);
    }

    [Fact]
    public async Task NormalizeAsync_WhenNoOverlap_OmitsApplicableTopologyNodeIds()
    {
        PolicyReferenceConnector sut = new();
        RawContextPayload raw = new()
        {
            PolicyReferences = ["SOC2"],
            TopologyHints = ["unrelated-vnet"]
        };

        NormalizedContextBatch batch = await sut.NormalizeAsync(raw, CancellationToken.None);

        batch.CanonicalObjects.Single().Properties.Should().NotContainKey("applicableTopologyNodeIds");
    }
}
