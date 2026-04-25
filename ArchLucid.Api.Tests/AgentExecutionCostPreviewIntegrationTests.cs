using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>Anonymous <c>GET /v1/agent-execution/cost-preview</c> contract (wizard cost card).</summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class AgentExecutionCostPreviewIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null, true) }
    };

    [Fact]
    public async Task GetCostPreview_when_mode_is_simulator_returns_null_estimate()
    {
        using OpenApiContractWebAppFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/agent-execution/cost-preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AgentExecutionCostPreviewDto? body = await response.Content.ReadFromJsonAsync<AgentExecutionCostPreviewDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.Mode.Should().Be("Simulator");
        body.MaxCompletionTokens.Should().Be(4096);
        body.EstimatedCostUsd.Should().BeNull();
        body.DeploymentName.Should().BeNull();
    }

    [Fact]
    public async Task GetCostPreview_when_mode_is_real_returns_estimate_and_deployment()
    {
        using RealModeOpenApiContractWebAppFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/agent-execution/cost-preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AgentExecutionCostPreviewDto? body = await response.Content.ReadFromJsonAsync<AgentExecutionCostPreviewDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.Mode.Should().Be("Real");
        body.MaxCompletionTokens.Should().Be(1024);
        body.DeploymentName.Should().Be("gpt-test-deploy");
        body.EstimatedCostUsd.Should().NotBeNull();
        // Default LlmCostEstimation rates: 8192 in @ 0.5/M + 1024 out @ 1.5/M
        body.EstimatedCostUsd!.Value.Should().BeApproximately(0.005632, 0.000001);
    }

    private sealed class AgentExecutionCostPreviewDto
    {
        public string Mode
        {
            get;
            set;
        } = "";

        public int MaxCompletionTokens
        {
            get;
            set;
        }

        public double? EstimatedCostUsd
        {
            get;
            set;
        }

        public string? DeploymentName
        {
            get;
            set;
        }
    }
}
