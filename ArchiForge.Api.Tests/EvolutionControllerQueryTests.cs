using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchiForge.Api.Models.Evolution;
using ArchiForge.Api.ProblemDetails;

using FluentAssertions;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchiForge.Api.Tests;

/// <summary>Read and validation paths for <c>/v1/evolution/*</c> on a dedicated factory instance (no seeding).</summary>
[Trait("Category", "Integration")]
[Trait("ChangeSet", "60R")]
public sealed class EvolutionControllerQueryTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListCandidates_Default_ReturnsOk_Empty()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/evolution/candidates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionCandidateChangeSetListResponse? body =
            await response.Content.ReadFromJsonAsync<EvolutionCandidateChangeSetListResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Candidates.Should().NotBeNull();
        body.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task ListCandidates_InvalidMax_Returns400Problem()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/evolution/candidates?max=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public async Task GetCandidate_Unknown_Returns404Problem()
    {
        HttpResponseMessage response =
            await Client.GetAsync("/v1/evolution/candidates/00000000-0000-0000-0000-000000000002");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.EvolutionCandidateChangeSetNotFound);
    }

    [Fact]
    public async Task CreateFromPlan_UnknownPlan_Returns404Problem()
    {
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/evolution/candidates/from-plan/00000000-0000-0000-0000-000000000001",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.LearningImprovementPlanNotFound);
    }
}
