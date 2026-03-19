using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureControllerTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateRun_ReturnsRun()
    {
        var request = TestRequestFactory.CreateArchitectureRequest("REQ-API-001");

        var response = await Client.PostAsync("/v1/architecture/request", JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Run.Should().NotBeNull();
        payload.Run.RunId.Should().NotBeNullOrWhiteSpace();
        payload.Run.RequestId.Should().Be("REQ-API-001");
        payload.Tasks.Should().HaveCount(4);
        payload.Tasks.Select(t => t.AgentType).Should().Contain([
            "Topology",
            "Cost",
            "Compliance",
            "Critic"
        ]);
    }

    [Fact]
    public async Task GetRun_ReturnsTasks()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-002")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();

        var runId = created!.Run.RunId;

        var getResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await getResponse.Content.ReadFromJsonAsync<GetRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Run.RunId.Should().Be(runId);
        payload.Tasks.Should().HaveCount(4);
        payload.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteRun_SeedsResults()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-003")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);

        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/execute",
            content: null);

        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var executePayload = await executeResponse.Content.ReadFromJsonAsync<ExecuteRunResponseDto>(JsonOptions);
        executePayload.Should().NotBeNull();
        executePayload!.RunId.Should().Be(runId);
        executePayload.Results.Should().HaveCount(4);

        var getRunResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");
        getRunResponse.EnsureSuccessStatusCode();

        var getRunPayload = await getRunResponse.Content.ReadFromJsonAsync<GetRunResponseDto>(JsonOptions);
        getRunPayload!.Results.Should().HaveCount(4);
    }

    [Fact]
    public async Task CommitRun_CreatesManifest()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-004")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        commitPayload.Should().NotBeNull();
        commitPayload!.Manifest.Should().NotBeNull();
        commitPayload.Manifest.RunId.Should().Be(runId);
        commitPayload.Manifest.SystemName.Should().Be("EnterpriseRag");
        commitPayload.Manifest.Services.Should().NotBeEmpty();
        commitPayload.Manifest.Datastores.Should().NotBeEmpty();
        commitPayload.Manifest.Governance.RequiredControls.Should().Contain(c =>
            c.Equals("Managed Identity", StringComparison.OrdinalIgnoreCase));
        commitPayload.DecisionTraces.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteRun_ExecutesTasksAndReturnsResults()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-EXEC")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);

        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var executePayload = await executeResponse.Content.ReadFromJsonAsync<ExecuteRunResponseDto>(JsonOptions);
        executePayload.Should().NotBeNull();
        executePayload!.RunId.Should().Be(runId);
        executePayload.Results.Should().HaveCount(4);
    }

    [Fact]
    public async Task GoldenPath_EndToEnd_WithExecute()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-006")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();

        var runId = created!.Run.RunId;
        runId.Should().NotBeNullOrWhiteSpace();

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        commitPayload.Should().NotBeNull();

        var manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;
        manifestVersion.Should().Be("v1");

        var manifestResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}");
        manifestResponse.EnsureSuccessStatusCode();

        var manifestPayload = await manifestResponse.Content.ReadFromJsonAsync<ManifestDto>(JsonOptions);
        manifestPayload.Should().NotBeNull();
        manifestPayload!.SystemName.Should().Be("EnterpriseRag");
        manifestPayload.Services.Should().NotBeEmpty();
        manifestPayload.Datastores.Should().NotBeEmpty();
        manifestPayload.Governance.RequiredControls.Should().NotBeEmpty();
        manifestPayload.Metadata.DecisionTraceIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GoldenPath_EndToEnd()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-005")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();

        var runId = created!.Run.RunId;
        runId.Should().NotBeNullOrWhiteSpace();

        var getRunResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");
        getRunResponse.EnsureSuccessStatusCode();

        var getRunPayload = await getRunResponse.Content.ReadFromJsonAsync<GetRunResponseDto>(JsonOptions);
        getRunPayload!.Tasks.Should().HaveCount(4);

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        commitPayload.Should().NotBeNull();

        var manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;
        manifestVersion.Should().Be("v1");

        var manifestResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}");
        manifestResponse.EnsureSuccessStatusCode();

        var manifestPayload = await manifestResponse.Content.ReadFromJsonAsync<ManifestDto>(JsonOptions);
        manifestPayload.Should().NotBeNull();
        manifestPayload!.SystemName.Should().Be("EnterpriseRag");
        manifestPayload.Services.Should().Contain(s => s.ServiceName == "rag-api");
        manifestPayload.Services.Should().Contain(s => s.ServiceName == "rag-search");
        manifestPayload.Datastores.Should().Contain(d => d.DatastoreName == "rag-metadata");
        manifestPayload.Governance.RequiredControls.Should().Contain(c =>
            c.Equals("Private Endpoints", StringComparison.OrdinalIgnoreCase));
        manifestPayload.Metadata.DecisionTraceIds.Should().NotBeEmpty();
    }
}