using ArchLucid.ContextIngestion.Infrastructure;
using ArchLucid.ContextIngestion.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.ContextIngestion.Tests;

public sealed class TerraformShowJsonInfrastructureDeclarationParserTests
{
    private readonly TerraformShowJsonInfrastructureDeclarationParser _sut =
        new(NullLogger<TerraformShowJsonInfrastructureDeclarationParser>.Instance);

    [Fact]
    public async Task ParseAsync_extracts_root_module_resources()
    {
        InfrastructureDeclarationReference decl = new()
        {
            Name = "state",
            Format = "terraform-show-json",
            DeclarationId = "d1",
            Content = """
                      {
                        "format_version": "1.0",
                        "values": {
                          "root_module": {
                            "resources": [
                              {
                                "address": "azurerm_resource_group.main",
                                "mode": "managed",
                                "type": "azurerm_resource_group",
                                "name": "main",
                                "provider_name": "registry.terraform.io/hashicorp/azurerm",
                                "values": { "location": "eastus", "name": "rg-demo" }
                              }
                            ]
                          }
                        }
                      }
                      """
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().ContainSingle();
        CanonicalObject o = objects[0];
        o.ObjectType.Should().Be("TopologyResource");
        o.Name.Should().Be("azurerm_resource_group.main");
        o.Properties.Should().ContainKey("terraformType");
        o.Properties["terraformType"].Should().Be("azurerm_resource_group");
        o.Properties.Should().ContainKey("tf.location");
    }

    [Fact]
    public async Task ParseAsync_maps_key_vault_to_security_baseline()
    {
        InfrastructureDeclarationReference decl = new()
        {
            Name = "state",
            Format = "terraform-show-json",
            DeclarationId = "d2",
            Content = """
                      {
                        "values": {
                          "root_module": {
                            "resources": [
                              {
                                "type": "azurerm_key_vault",
                                "name": "core",
                                "provider_name": "registry.terraform.io/hashicorp/azurerm",
                                "mode": "managed",
                                "values": { "name": "kv1" }
                              }
                            ]
                          }
                        }
                      }
                      """
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().ContainSingle();
        objects[0].ObjectType.Should().Be("SecurityBaseline");
    }

    [Fact]
    public async Task ParseAsync_empty_values_returns_empty()
    {
        InfrastructureDeclarationReference decl = new()
        {
            Name = "bad", Format = "terraform-show-json", DeclarationId = "d3", Content = "{}"
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_collects_child_module_resources()
    {
        InfrastructureDeclarationReference decl = new()
        {
            Name = "state",
            Format = "terraform-show-json",
            DeclarationId = "d4",
            Content = """
                      {
                        "values": {
                          "root_module": {
                            "resources": [],
                            "child_modules": [
                              {
                                "resources": [
                                  {
                                    "type": "azurerm_storage_account",
                                    "name": "data",
                                    "mode": "managed",
                                    "provider_name": "registry.terraform.io/hashicorp/azurerm",
                                    "values": { "name": "stacct" }
                                  }
                                ]
                              }
                            ]
                          }
                        }
                      }
                      """
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().ContainSingle();
        objects[0].Name.Should().Be("azurerm_storage_account.data");
        objects[0].Properties.Should().ContainKey("mode");
    }

    [Fact]
    public async Task ParseAsync_resolves_type_after_provider_slash()
    {
        InfrastructureDeclarationReference decl = new()
        {
            Name = "state",
            Format = "terraform-show-json",
            DeclarationId = "d5",
            Content = """
                      {
                        "values": {
                          "root_module": {
                            "resources": [
                              {
                                "type": "registry.terraform.io/hashicorp/azurerm/azurerm_network_security_group",
                                "name": "nsg",
                                "values": {}
                              }
                            ]
                          }
                        }
                      }
                      """
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().ContainSingle();
        objects[0].ObjectType.Should().Be("SecurityBaseline");
    }

    [Fact]
    public async Task ParseAsync_maps_policy_assignment_type()
    {
        InfrastructureDeclarationReference decl = new()
        {
            Name = "state",
            Format = "terraform-show-json",
            DeclarationId = "d6",
            Content = """
                      {
                        "values": {
                          "root_module": {
                            "resources": [
                              {
                                "type": "azurerm_policy_assignment",
                                "name": "audit",
                                "values": { "name": "pa1" }
                              }
                            ]
                          }
                        }
                      }
                      """
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().ContainSingle();
        objects[0].ObjectType.Should().Be("PolicyControl");
    }

    [Fact]
    public async Task ParseAsync_serializes_numeric_and_boolean_value_kinds()
    {
        InfrastructureDeclarationReference decl = new()
        {
            Name = "state",
            Format = "terraform-show-json",
            DeclarationId = "d7",
            Content = """
                      {
                        "values": {
                          "root_module": {
                            "resources": [
                              {
                                "type": "azurerm_resource_group",
                                "name": "x",
                                "values": {
                                  "sku": 42,
                                  "enabled": true,
                                  "disabled": false,
                                  "nested": { "a": 1 },
                                  "weird name!": "v"
                                }
                              }
                            ]
                          }
                        }
                      }
                      """
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().ContainSingle();
        CanonicalObject o = objects[0];
        o.Properties.Should().ContainKey("tf.sku");
        o.Properties["tf.sku"].Should().Be("42");
        o.Properties["tf.enabled"].Should().Be("true");
        o.Properties["tf.disabled"].Should().Be("false");
        o.Properties.Should().ContainKey("tf.weird_name_");
    }

    [Fact]
    public async Task ParseAsync_truncates_long_attribute_values()
    {
        string longText = new('x', 600);
        InfrastructureDeclarationReference decl = new()
        {
            Name = "state",
            Format = "terraform-show-json",
            DeclarationId = "d8",
            Content = $$"""
                        {
                          "values": {
                            "root_module": {
                              "resources": [
                                {
                                  "type": "azurerm_resource_group",
                                  "name": "x",
                                  "values": { "big": "{{longText}}" }
                                }
                              ]
                            }
                          }
                        }
                        """
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().ContainSingle();
        objects[0].Properties["tf.big"].Length.Should().Be(512);
    }

    [Fact]
    public async Task ParseAsync_whitespace_content_returns_empty()
    {
        InfrastructureDeclarationReference decl = new()
        {
            Name = "empty", Format = "terraform-show-json", DeclarationId = "d9", Content = "   "
        };

        IReadOnlyList<CanonicalObject> objects = await _sut.ParseAsync(decl, CancellationToken.None);

        objects.Should().BeEmpty();
    }
}
