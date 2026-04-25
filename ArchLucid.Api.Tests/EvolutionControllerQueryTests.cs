using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Models.Evolution;

using FluentAssertions;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchLucid.Api.Tests;

/// <summary>Read and validation paths for <c>/v1/evolution/*</c> on a dedicated factory instance (no seeding).</summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
[Trait("ChangeSet", "60R")]
public sealed class EvolutionControllerQueryTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListCandidates_Default_ReturnsOk_Empty()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/evolution/candidates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionCandidateChangeSetListResponse? body =
            await response.Content.ReadFromJsonAsync<EvolutionCandidateChangeSetListResponse>(JsonOptions);

        body.Should().NotBeNull();
        body.Candidates.Should().NotBeNull();
        body.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task ListCandidates_InvalidMax_Returns400Problem()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/evolution/candidates?max=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public async Task GetCandidate_Unknown_Returns404Problem()
    {
        HttpResponseMessage response =
            await Client.GetAsync("/v1/evolution/candidates/00000000-0000-0000-0000-000000000002");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.EvolutionCandidateChangeSetNotFound);
    }

    [Fact]
    public async Task GetResults_UnknownCandidate_Returns404Problem()
    {
        HttpResponseMessage response =
            await Client.GetAsync("/v1/evolution/results/00000000-0000-0000-0000-000000000099");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.EvolutionCandidateChangeSetNotFound);
    }

    [Fact]
    public async Task ExportResults_UnknownCandidate_Returns404Problem()
    {
        HttpResponseMessage response = await Client.GetAsync(
            "/v1/evolution/results/00000000-0000-0000-0000-000000000088/export?format=markdown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.EvolutionCandidateChangeSetNotFound);
    }

    [Fact]
    public async Task ExportResults_InvalidFormat_Returns400Problem()
    {
        Guid candidateId = Guid.Parse("00000000-0000-0000-0000-000000000077");
        HttpResponseMessage response =
            await Client.GetAsync($"/v1/evolution/results/{candidateId:D}/export?format=xml");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public async Task CreateFromPlan_UnknownPlan_Returns404Problem()
    {
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/evolution/candidates/from-plan/00000000-0000-0000-0000-000000000001",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.LearningImprovementPlanNotFound);
    }

    [Fact]
    public async Task Simulate_UnknownCandidate_Returns404Problem()
    {
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/evolution/simulate/00000000-0000-0000-0000-0000000000aa",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.EvolutionCandidateChangeSetNotFound);
    }

    [Fact]
    public async Task ExportResults_OmittedFormat_UnknownCandidate_Returns404Problem()
    {
        HttpResponseMessage response = await Client.GetAsync(
            "/v1/evolution/results/00000000-0000-0000-0000-0000000000bb/export");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.EvolutionCandidateChangeSetNotFound);
    }
}
