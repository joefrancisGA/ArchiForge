using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Problem-details branches on <see cref="ArchLucid.Api.Controllers.Authority.AuthorityQueryController" />:
///     validation (whitespace project id), missing run summary, and provenance preconditions (422).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class AuthorityQueryControllerProblemDetailsIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData("%20")]
    [InlineData("%09")]
    public async Task ListRunsByProject_whitespace_project_id_returns_400_problem(string encodedWhitespaceProjectId)
    {
        HttpResponseMessage response =
            await Client.GetAsync($"/v1/authority/projects/{encodedWhitespaceProjectId}/runs");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        string body = await response.Content.ReadAsStringAsync();
        body.ToLowerInvariant().Should().Contain("projectid");
    }

    [Fact]
    public async Task GetRunSummary_unknown_run_returns_404_problem()
    {
        Guid missing = Guid.Parse("00000000-0000-0000-0000-00000000cc01");
        HttpResponseMessage response = await Client.GetAsync($"/v1/authority/runs/{missing:D}/summary");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.RunNotFound);
    }

    [Fact]
    public async Task GetRunProvenance_when_run_lacks_snapshot_chain_returns_422_problem()
    {
        Guid runId =
            await AdvisoryIntegrationSeed.SeedDefaultScopeAuthorityRunAsync(Factory.Services, CancellationToken.None);

        HttpResponseMessage response = await Client.GetAsync($"/v1/authority/runs/{runId:D}/provenance");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.ValidationFailed);
        problem.Detail.Should().Contain("Provenance requires");
    }
}
