using ArchiForge.ContextIngestion.Infrastructure;
using ArchiForge.ContextIngestion.Models;

using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

[Trait("Suite", "Core")]
public sealed class SimpleTerraformDeclarationParserTests
{
    private readonly SimpleTerraformDeclarationParser _sut = new();

    [Fact]
    public async Task ParseAsync_ExtractsResourceBlocks()
    {
        InfrastructureDeclarationReference declaration = new()
        {
            Name = "stub.tf",
            Format = "simple-terraform",
            Content = """
                resource "azurerm_virtual_network" "core"
                resource "azurerm_subnet" "app"
                resource "azurerm_storage_account" "docs"
                resource "azurerm_linux_web_app" "api"
                resource "azurerm_key_vault" "kv"
                """
        };

        IReadOnlyList<CanonicalObject> result = await _sut.ParseAsync(declaration, CancellationToken.None);

        result.Should().HaveCount(5);
        result.Should().ContainSingle(o => o.Name == "kv" && o.ObjectType == "SecurityBaseline");
        result.Should().ContainSingle(o => o.Name == "core" && o.Properties["terraformType"] == "azurerm_virtual_network");
    }
}
