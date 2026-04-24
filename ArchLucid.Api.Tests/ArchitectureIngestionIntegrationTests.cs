using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Integration tests: Architecture Ingestion (HTTP host, database, or cross-component).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureIngestionIntegrationTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateRun_WithIngestionFields_ReturnsCreated()
    {
        var request = new
        {
            requestId = "REQ-INGEST-INT-001",
            description =
                "Design a secure Azure RAG system for enterprise internal documents using Azure AI Search, managed identity, private endpoints, SQL metadata storage, and moderate cost sensitivity.",
            systemName = "IngestionIntegrationSys",
            environment = "prod",
            cloudProvider = 1,
            constraints = new[] { "Private endpoints required" },
            requiredCapabilities = new[] { "Azure AI Search", "SQL" },
            assumptions = Array.Empty<string>(),
            priorManifestVersion = (string?)null,
            inlineRequirements = new[] { "Must emit structured audit logs" },
            documents =
                new[]
                {
                    new
                    {
                        name = "policy-hints.txt",
                        contentType = "text/plain",
                        content = "REQ: Multi-region failover\nPOL: Data classification enforced"
                    }
                },
            policyReferences = new[] { "ORG-POL-42" },
            topologyHints = new[] { "hub-spoke with shared services subnet" },
            securityBaselineHints = new[] { "TLS 1.2 minimum for all endpoints" },
            infrastructureDeclarations = new object[]
            {
                new
                {
                    name = "core.json",
                    format = "json",
                    content =
                        """{"resources":[{"type":"vnet","name":"core-vnet","region":"eastus","properties":{"addressSpace":"10.0.0.0/16"}}]}"""
                },
                new
                {
                    name = "stub.tf",
                    format = "simple-terraform",
                    content = """
                              resource "azurerm_virtual_network" "core"
                              resource "azurerm_key_vault" "kv"
                              """
                }
            }
        };

        HttpResponseMessage response = await Client.PostAsync("/v1/architecture/request", JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CreateRunResponseDto? payload = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Run.RunId.Should().NotBeNullOrWhiteSpace();
        payload.Tasks.Should().NotBeEmpty();
    }
}
