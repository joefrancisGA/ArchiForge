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
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null) }
    };

    [SkippableFact]
    public async Task GetCostPreview_when_mode_is_simulator_returns_null_estimates_and_basis()
    {
        await using OpenApiContractWebAppFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/agent-execution/cost-preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AgentExecutionCostPreviewDto? body =
            await response.Content.ReadFromJsonAsync<AgentExecutionCostPreviewDto>(JsonOptions);
        body.Should().NotBeNull();
        body.Mode.Should().Be("Simulator");
        body.MaxCompletionTokens.Should().Be(4096);
        body.EstimatedCostUsd.Should().BeNull();
        body.EstimatedCostUsdLow.Should().BeNull();
        body.EstimatedCostUsdHigh.Should().BeNull();
        body.DeploymentName.Should().BeNull();
        body.EstimatedCostBasis.Should().NotBeNullOrWhiteSpace();
        body.PricingUsesIllustrativeUsdRates.Should().BeTrue();
    }

    [SkippableFact]
    public async Task GetCostPreview_when_mode_is_real_returns_range_estimate_and_deployment()
    {
        await using RealModeOpenApiContractWebAppFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/agent-execution/cost-preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AgentExecutionCostPreviewDto? body =
            await response.Content.ReadFromJsonAsync<AgentExecutionCostPreviewDto>(JsonOptions);
        body.Should().NotBeNull();
        body.Mode.Should().Be("Real");
        body.MaxCompletionTokens.Should().Be(1024);
        body.DeploymentName.Should().Be("gpt-test-deploy");
        body.EstimatedCostUsd.Should().NotBeNull();
        body.EstimatedCostUsdLow.Should().NotBeNull();
        body.EstimatedCostUsdHigh.Should().NotBeNull();
        body.EstimatedCostUsd.Should().Be(body.EstimatedCostUsdHigh);
        body.EstimatedCostBasis.Should().Contain("65536");
        body.PricingUsesIllustrativeUsdRates.Should().BeTrue();

        // Default LlmCostEstimation rates: low = one completion at 8192 in @ 0.5/M + 1024 out @ 1.5/M
        body.EstimatedCostUsdLow!.Value.Should().BeApproximately(0.005632, 0.000001);

        // High = 4 Ã— (65536 in @ 0.5/M + 1024 out @ 1.5/M)
        body.EstimatedCostUsdHigh!.Value.Should().BeApproximately(0.137216, 0.000001);
    }

    private sealed class AgentExecutionCostPreviewDto
    {
        public string Mode
        {
            get;
            init;
        } = "";

        public int MaxCompletionTokens
        {
            get;
            init;
        }

        public double? EstimatedCostUsd
        {
            get;
            init;
        }

        public double? EstimatedCostUsdLow
        {
            get;
            init;
        }

        public double? EstimatedCostUsdHigh
        {
            get;
            init;
        }

        public string EstimatedCostBasis
        {
            get;
            init;
        } = "";

        public bool PricingUsesIllustrativeUsdRates
        {
            get;
            init;
        }

        public string? DeploymentName
        {
            get;
            init;
        }
    }
}
