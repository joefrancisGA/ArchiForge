using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Models.Evolution;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchLucid.Api.Tests;

/// <summary>Execute paths for <c>/v1/evolution/*</c> (separate fixture from query tests so list endpoints stay isolated).</summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
[Trait("ChangeSet", "60R")]
public sealed class EvolutionControllerFlowTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task ShadowEvaluate_UnknownCandidate_Returns404Problem()
    {
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/evolution/candidates/00000000-0000-0000-0000-000000000003/shadow-evaluate",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.EvolutionCandidateChangeSetNotFound);
    }

    [SkippableFact]
    public async Task PlanWithNoLinkedRuns_CreateShadowGet_YieldsSimulatedCandidateWithNoSimulationRows()
    {
        Guid planId = await SeedMinimalPlanInDefaultScopeAsync();

        HttpResponseMessage createResponse =
            await Client.PostAsync($"/v1/evolution/candidates/from-plan/{planId}", null);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionCandidateChangeSetResponse? created =
            await createResponse.Content.ReadFromJsonAsync<EvolutionCandidateChangeSetResponse>(JsonOptions);

        created.Should().NotBeNull();
        created.SourcePlanId.Should().Be(planId);
        created.Status.Should().Be(EvolutionCandidateChangeSetStatusValues.Draft);
        created.DerivationRuleVersion.Should().Be("60R-v1");

        HttpResponseMessage shadowResponse =
            await Client.PostAsync(
                $"/v1/evolution/candidates/{created.CandidateChangeSetId}/shadow-evaluate",
                null);

        shadowResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionShadowEvaluateResponse? shadowBody =
            await shadowResponse.Content.ReadFromJsonAsync<EvolutionShadowEvaluateResponse>(JsonOptions);

        shadowBody.Should().NotBeNull();
        shadowBody.SimulationRuns.Should().BeEmpty();

        HttpResponseMessage detailResponse =
            await Client.GetAsync($"/v1/evolution/candidates/{created.CandidateChangeSetId}");

        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionCandidateDetailResponse? detail =
            await detailResponse.Content.ReadFromJsonAsync<EvolutionCandidateDetailResponse>(JsonOptions);

        detail.Should().NotBeNull();
        detail.Candidate.Status.Should().Be(EvolutionCandidateChangeSetStatusValues.Simulated);
        detail.SimulationRuns.Should().BeEmpty();
        detail.PlanSnapshotJson.Should().NotBeNullOrWhiteSpace();

        JsonDocument.Parse(detail.PlanSnapshotJson);
    }

    [SkippableFact]
    public async Task Simulate_ThenGetResults_NoLinkedRuns_ReturnsCandidateAndEmptyRuns()
    {
        Guid planId = await SeedMinimalPlanInDefaultScopeAsync();

        HttpResponseMessage createResponse =
            await Client.PostAsync($"/v1/evolution/candidates/from-plan/{planId}", null);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionCandidateChangeSetResponse? created =
            await createResponse.Content.ReadFromJsonAsync<EvolutionCandidateChangeSetResponse>(JsonOptions);

        created.Should().NotBeNull();

        HttpResponseMessage simulateResponse = await Client.PostAsync(
            $"/v1/evolution/simulate/{created.CandidateChangeSetId}",
            null);

        simulateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionSimulateResponse? simBody =
            await simulateResponse.Content.ReadFromJsonAsync<EvolutionSimulateResponse>(JsonOptions);

        simBody.Should().NotBeNull();
        simBody.Candidate.CandidateChangeSetId.Should().Be(created.CandidateChangeSetId);
        simBody.SimulationRuns.Should().BeEmpty();

        HttpResponseMessage resultsResponse =
            await Client.GetAsync($"/v1/evolution/results/{created.CandidateChangeSetId}");

        resultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionResultsResponse? resultsBody =
            await resultsResponse.Content.ReadFromJsonAsync<EvolutionResultsResponse>(JsonOptions);

        resultsBody.Should().NotBeNull();
        resultsBody.Candidate.CandidateChangeSetId.Should().Be(created.CandidateChangeSetId);
        resultsBody.PlanSnapshotJson.Should().NotBeNullOrWhiteSpace();
        resultsBody.SimulationRuns.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task ExportResults_MarkdownAndJson_ContainDescriptionDiffAndSchema()
    {
        Guid planId = await SeedMinimalPlanInDefaultScopeAsync();

        HttpResponseMessage createResponse =
            await Client.PostAsync($"/v1/evolution/candidates/from-plan/{planId}", null);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionCandidateChangeSetResponse? created =
            await createResponse.Content.ReadFromJsonAsync<EvolutionCandidateChangeSetResponse>(JsonOptions);

        created.Should().NotBeNull();

        HttpResponseMessage simulateResponse = await Client.PostAsync(
            $"/v1/evolution/simulate/{created.CandidateChangeSetId}",
            null);

        simulateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Guid candidateId = created.CandidateChangeSetId;

        HttpResponseMessage markdownResponse =
            await Client.GetAsync($"/v1/evolution/results/{candidateId:D}/export?format=markdown");

        markdownResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string markdown = await markdownResponse.Content.ReadAsStringAsync();
        markdown.Should().Contain("Evolution test plan");
        markdown.Should().Contain("60R-simulation-export-v1");
        markdown.Should().Contain("## Simulation results");
        markdown.Should().Contain("No simulation rows persisted for this candidate.");

        HttpResponseMessage jsonResponse =
            await Client.GetAsync($"/v1/evolution/results/{candidateId:D}/export?format=json");

        jsonResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await jsonResponse.Content.ReadAsStringAsync();
        json.Should().Contain("Evolution test plan");
        json.Should().Contain("60R-simulation-export-v1");
        json.Should().Contain("\"simulationRuns\"");

        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("simulationRuns").GetArrayLength().Should().Be(0);
    }

    private async Task<Guid> SeedMinimalPlanInDefaultScopeAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        IProductLearningPlanningRepository planning =
            scope.ServiceProvider.GetRequiredService<IProductLearningPlanningRepository>();

        Guid themeId = Guid.NewGuid();
        string themeKey = "evolution-test-" + themeId.ToString("N");

        await planning.InsertThemeAsync(
            new ProductLearningImprovementThemeRecord
            {
                ThemeId = themeId,
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject,
                ThemeKey = themeKey,
                Title = "Evolution test theme",
                Summary = "Seeded for 60R API tests.",
                AffectedArtifactTypeOrWorkflowArea = "Test",
                SeverityBand = "Low",
                DerivationRuleVersion = "test"
            },
            CancellationToken.None);

        Guid planId = Guid.NewGuid();

        await planning.InsertPlanAsync(
            new ProductLearningImprovementPlanRecord
            {
                PlanId = planId,
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject,
                ThemeId = themeId,
                Title = "Evolution test plan",
                Summary = "No linked runs; shadow path completes without simulation rows.",
                ActionSteps =
                [
                    new ProductLearningImprovementPlanActionStep
                    {
                        Ordinal = 1, ActionType = "Observe", Description = "Observe only."
                    }
                ]
            },
            CancellationToken.None);

        return planId;
    }
}
