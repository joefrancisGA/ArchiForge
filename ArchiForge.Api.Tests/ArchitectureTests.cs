using System.Text.Json;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Requests;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.DecisionEngine.Validation;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public class ArchitectureTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly DecisionEngineService Engine = new(new PassthroughSchemaValidationService());

    [Fact]
    public async Task CreateArchitectureRun_ShouldReturnRunId()
    {
        var request = new
        {
            requestId = "REQ-TEST-1",
            description = "Test architecture",
            systemName = "TestSystem",
            environment = "dev",
            cloudProvider = 1
        };

        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(request));

        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("runId");
    }

    [Fact]
    public async Task GoldenPath_ShouldProduceManifest()
    {
        var request = new
        {
            requestId = "REQ-1",
            description = "Test architecture",
            systemName = "TestSystem",
            environment = "dev",
            cloudProvider = 1
        };

        HttpResponseMessage create = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(request));

        create.EnsureSuccessStatusCode();

        string body = await create.Content.ReadAsStringAsync();

        string? runId = JsonDocument.Parse(body)
            .RootElement
            .GetProperty("run")
            .GetProperty("runId")
            .GetString();

        HttpResponseMessage seed = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/seed-fake-results",
            null);

        seed.EnsureSuccessStatusCode();

        HttpResponseMessage commit = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/commit",
            null);

        commit.EnsureSuccessStatusCode();

        HttpResponseMessage manifest = await Client.GetAsync(
            "/v1/architecture/manifest/v1");

        manifest.EnsureSuccessStatusCode();
    }

    [Fact]
    public void DecisionEngine_FixtureScenario_ProducesExpectedArchitecture()
    {
        ArchitectureRequest request =
            FixtureLoader.Load<ArchitectureRequest>(
                "requests/enterprise-rag-request.json");

        AgentResult topology =
            FixtureLoader.Load<AgentResult>(
                "results/topology-result.json");

        AgentResult cost =
            FixtureLoader.Load<AgentResult>(
                "results/cost-result.json");

        AgentResult compliance =
            FixtureLoader.Load<AgentResult>(
                "results/compliance-result.json");

        ExpectedManifestSummary expected =
            FixtureLoader.Load<ExpectedManifestSummary>(
                "expected/expected-manifest-summary.json");

        DecisionMergeResult result = Engine.MergeResults(
            "RUN-FIXTURE",
            request,
            "v1",
            [topology, cost, compliance],
            evaluations: [],
            decisionNodes: []);

        result.Success.Should().BeTrue();

        result.Manifest.Services
            .Select(s => s.ServiceName)
            .Should()
            .Contain(expected.Services);

        result.Manifest.Datastores
            .Select(d => d.DatastoreName)
            .Should()
            .Contain(expected.Datastores);

        result.Manifest.Governance.RequiredControls
            .Should()
            .Contain(expected.RequiredControls);
    }
}
