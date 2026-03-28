using System.Net;
using System.Net.Http.Json;

using ArchiForge.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureControllerTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateRun_ReturnsRun()
    {
        object request = TestRequestFactory.CreateArchitectureRequest("REQ-API-001");

        HttpResponseMessage response = await Client.PostAsync("/v1/architecture/request", JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        CreateRunResponseDto? payload = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Run.Should().NotBeNull();
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
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-002")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();

        string runId = created.Run.RunId;

        HttpResponseMessage getResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        GetRunResponseDto? payload = await getResponse.Content.ReadFromJsonAsync<GetRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Run.RunId.Should().Be(runId);
        payload.Tasks.Should().HaveCount(4);
        payload.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteRun_SeedsResults()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-003")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);

        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/execute",
            content: null);

        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ExecuteRunResponseDto? executePayload = await executeResponse.Content.ReadFromJsonAsync<ExecuteRunResponseDto>(JsonOptions);
        executePayload.Should().NotBeNull();
        executePayload.RunId.Should().Be(runId);
        executePayload.Results.Should().HaveCount(4);

        HttpResponseMessage getRunResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");
        getRunResponse.EnsureSuccessStatusCode();

        GetRunResponseDto? getRunPayload = await getRunResponse.Content.ReadFromJsonAsync<GetRunResponseDto>(JsonOptions);
        getRunPayload!.Results.Should().HaveCount(4);
    }

    [Fact]
    public async Task CommitRun_CreatesManifest()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-004")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        CommitRunResponseDto? commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        commitPayload.Should().NotBeNull();
        commitPayload.Manifest.Should().NotBeNull();
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
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-EXEC")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();
        string runId = created.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);

        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ExecuteRunResponseDto? executePayload = await executeResponse.Content.ReadFromJsonAsync<ExecuteRunResponseDto>(JsonOptions);
        executePayload.Should().NotBeNull();
        executePayload.RunId.Should().Be(runId);
        executePayload.Results.Should().HaveCount(4);
    }

    [Fact]
    public async Task GoldenPath_EndToEnd_WithExecute()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-006")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();

        string runId = created.Run.RunId;
        runId.Should().NotBeNullOrWhiteSpace();

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        CommitRunResponseDto? commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        commitPayload.Should().NotBeNull();

        string manifestVersion = commitPayload.Manifest.Metadata.ManifestVersion;
        manifestVersion.Should().Be("v1");

        HttpResponseMessage manifestResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}");
        manifestResponse.EnsureSuccessStatusCode();

        ManifestDto? manifestPayload = await manifestResponse.Content.ReadFromJsonAsync<ManifestDto>(JsonOptions);
        manifestPayload.Should().NotBeNull();
        manifestPayload.SystemName.Should().Be("EnterpriseRag");
        manifestPayload.Services.Should().NotBeEmpty();
        manifestPayload.Datastores.Should().NotBeEmpty();
        manifestPayload.Governance.RequiredControls.Should().NotBeEmpty();
        manifestPayload.Metadata.DecisionTraceIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GoldenPath_EndToEnd()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-API-005")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();

        string runId = created.Run.RunId;
        runId.Should().NotBeNullOrWhiteSpace();

        HttpResponseMessage getRunResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");
        getRunResponse.EnsureSuccessStatusCode();

        GetRunResponseDto? getRunPayload = await getRunResponse.Content.ReadFromJsonAsync<GetRunResponseDto>(JsonOptions);
        getRunPayload!.Tasks.Should().HaveCount(4);

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        CommitRunResponseDto? commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        commitPayload.Should().NotBeNull();

        string manifestVersion = commitPayload.Manifest.Metadata.ManifestVersion;
        manifestVersion.Should().Be("v1");

        HttpResponseMessage manifestResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}");
        manifestResponse.EnsureSuccessStatusCode();

        ManifestDto? manifestPayload = await manifestResponse.Content.ReadFromJsonAsync<ManifestDto>(JsonOptions);
        manifestPayload.Should().NotBeNull();
        manifestPayload.SystemName.Should().Be("EnterpriseRag");
        manifestPayload.Services.Should().Contain(s => s.ServiceName == "rag-api");
        manifestPayload.Services.Should().Contain(s => s.ServiceName == "rag-search");
        manifestPayload.Datastores.Should().Contain(d => d.DatastoreName == "rag-metadata");
        manifestPayload.Governance.RequiredControls.Should().Contain(c =>
            c.Equals("Private Endpoints", StringComparison.OrdinalIgnoreCase));
        manifestPayload.Metadata.DecisionTraceIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateRun_SameIdempotencyKeyAndBody_SecondCallReturns200WithSameRunId()
    {
        string idempotencyKey = $"idem-{Guid.NewGuid():N}";
        object body = TestRequestFactory.CreateArchitectureRequest("REQ-IDEM-001");
        using HttpRequestMessage first = new(HttpMethod.Post, "/v1/architecture/request");
        first.Content = JsonContent(body);
        first.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        HttpResponseMessage firstResponse = await Client.SendAsync(first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        CreateRunResponseDto? firstPayload = await firstResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        firstPayload.Should().NotBeNull();

        using HttpRequestMessage second = new(HttpMethod.Post, "/v1/architecture/request");
        second.Content = JsonContent(body);
        second.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        HttpResponseMessage secondResponse = await Client.SendAsync(second);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.Headers.TryGetValues("Idempotency-Replayed", out IEnumerable<string>? replayValues).Should().BeTrue();
        replayValues.Should().Contain("true");

        CreateRunResponseDto? secondPayload = await secondResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        secondPayload.Should().NotBeNull();
        secondPayload.Run.RunId.Should().Be(firstPayload.Run.RunId);
    }

    [Fact]
    public async Task CreateRun_SameIdempotencyKeyDifferentBody_Returns409()
    {
        string idempotencyKey = $"idem-{Guid.NewGuid():N}";
        object firstBody = TestRequestFactory.CreateArchitectureRequest("REQ-IDEM-002A");
        using HttpRequestMessage first = new(HttpMethod.Post, "/v1/architecture/request");
        first.Content = JsonContent(firstBody);
        first.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        HttpResponseMessage firstResponse = await Client.SendAsync(first);
        firstResponse.EnsureSuccessStatusCode();

        object secondBody = TestRequestFactory.CreateArchitectureRequest("REQ-IDEM-002B");
        using HttpRequestMessage second = new(HttpMethod.Post, "/v1/architecture/request");
        second.Content = JsonContent(secondBody);
        second.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        HttpResponseMessage secondResponse = await Client.SendAsync(second);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
