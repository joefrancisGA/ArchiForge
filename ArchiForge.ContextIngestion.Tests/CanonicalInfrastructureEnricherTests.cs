using ArchiForge.ContextIngestion.Canonicalization;
using ArchiForge.ContextIngestion.Models;

using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

public sealed class CanonicalInfrastructureEnricherTests
{
    private readonly CanonicalInfrastructureEnricher _sut = new();

    [Fact]
    public void Enrich_AddsCategory_ForJsonResourceTypes()
    {
        List<CanonicalObject> items = new List<CanonicalObject>
        {
            new()
            {
                ObjectType = "TopologyResource",
                Name = "core-vnet",
                SourceType = "InfrastructureDeclaration",
                SourceId = "id",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["resourceType"] = "vnet",
                    ["region"] = "eastus"
                }
            }
        };

        IReadOnlyList<CanonicalObject> enriched = _sut.Enrich(items);

        enriched[0].Properties["category"].Should().Be("network");
    }

    [Fact]
    public void Enrich_AddsCategory_AndStatus_ForTerraformAndSecurity()
    {
        List<CanonicalObject> items = new List<CanonicalObject>
        {
            new()
            {
                ObjectType = "TopologyResource",
                Name = "core",
                SourceType = "InfrastructureDeclaration",
                SourceId = "id",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["terraformType"] = "azurerm_virtual_network"
                }
            },
            new()
            {
                ObjectType = "SecurityBaseline",
                Name = "kv",
                SourceType = "InfrastructureDeclaration",
                SourceId = "id",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["terraformType"] = "azurerm_key_vault"
                }
            }
        };

        IReadOnlyList<CanonicalObject> enriched = _sut.Enrich(items);

        enriched[0].Properties["category"].Should().Be("network");
        enriched[1].Properties["status"].Should().Be("declared");
    }
}
