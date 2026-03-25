using ArchiForge.ContextIngestion.Infrastructure;
using ArchiForge.ContextIngestion.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.ContextIngestion.Tests;

public sealed class JsonInfrastructureDeclarationParserTests
{
    private readonly JsonInfrastructureDeclarationParser _sut =
        new(NullLogger<JsonInfrastructureDeclarationParser>.Instance);

    [Fact]
    public async Task ParseAsync_MapsVnetSubnetStorageApp_KeyVault()
    {
        InfrastructureDeclarationReference declaration = new InfrastructureDeclarationReference
        {
            Name = "core.json",
            Format = "json",
            Content = """
            {
              "resources": [
                { "type": "vnet", "name": "core-vnet", "region": "eastus", "properties": { "addressSpace": "10.0.0.0/16" } },
                { "type": "subnet", "name": "app-subnet", "region": "eastus", "properties": { "cidr": "10.0.1.0/24" } },
                { "type": "storage", "name": "docstorage01", "region": "eastus", "properties": { "sku": "Standard_LRS" } },
                { "type": "appservice", "name": "archiforge-api", "region": "eastus", "properties": { "plan": "P1v3" } },
                { "type": "keyvault", "name": "archiforge-kv", "region": "eastus", "properties": {} }
              ]
            }
            """
        };

        IReadOnlyList<CanonicalObject> result = await _sut.ParseAsync(declaration, CancellationToken.None);

        result.Should().HaveCount(5);
        result.Count(o => o.ObjectType == "TopologyResource").Should().Be(4);
        result.Should().ContainSingle(o => o.ObjectType == "SecurityBaseline" && o.Name == "archiforge-kv");
        result.Should().Contain(o => o.Properties["resourceType"] == "vnet" && o.Properties["region"] == "eastus");
    }
}
