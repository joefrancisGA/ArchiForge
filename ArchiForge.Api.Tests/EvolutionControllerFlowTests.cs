using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchiForge.Api.Models.Evolution;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Contracts.Evolution;
using ArchiForge.Contracts.ProductLearning.Planning;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.ProductLearning.Planning;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchiForge.Api.Tests;

/// <summary>Execute paths for <c>/v1/evolution/*</c> (separate fixture from query tests so list endpoints stay isolated).</summary>
[Trait("Category", "Integration")]
[Trait("ChangeSet", "60R")]
public sealed class EvolutionControllerFlowTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ShadowEvaluate_UnknownCandidate_Returns404Problem()
    {
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/evolution/candidates/00000000-0000-0000-0000-000000000003/shadow-evaluate",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.EvolutionCandidateChangeSetNotFound);
    }

    [Fact]
    public async Task PlanWithNoLinkedRuns_CreateShadowGet_YieldsSimulatedCandidateWithNoSimulationRows()
    {
        Guid planId = await SeedMinimalPlanInDefaultScopeAsync();

        HttpResponseMessage createResponse =
            await Client.PostAsync($"/v1/evolution/candidates/from-plan/{planId}", content: null);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionCandidateChangeSetResponse? created =
            await createResponse.Content.ReadFromJsonAsync<EvolutionCandidateChangeSetResponse>(JsonOptions);

        created.Should().NotBeNull();
        created!.SourcePlanId.Should().Be(planId);
        created.Status.Should().Be(EvolutionCandidateChangeSetStatusValues.Draft);
        created.DerivationRuleVersion.Should().Be("60R-v1");

        HttpResponseMessage shadowResponse =
            await Client.PostAsync(
                $"/v1/evolution/candidates/{created.CandidateChangeSetId}/shadow-evaluate",
                content: null);

        shadowResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionShadowEvaluateResponse? shadowBody =
            await shadowResponse.Content.ReadFromJsonAsync<EvolutionShadowEvaluateResponse>(JsonOptions);

        shadowBody.Should().NotBeNull();
        shadowBody!.SimulationRuns.Should().BeEmpty();

        HttpResponseMessage detailResponse =
            await Client.GetAsync($"/v1/evolution/candidates/{created.CandidateChangeSetId}");

        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        EvolutionCandidateDetailResponse? detail =
            await detailResponse.Content.ReadFromJsonAsync<EvolutionCandidateDetailResponse>(JsonOptions);

        detail.Should().NotBeNull();
        detail!.Candidate.Status.Should().Be(EvolutionCandidateChangeSetStatusValues.Simulated);
        detail.SimulationRuns.Should().BeEmpty();
        detail.PlanSnapshotJson.Should().NotBeNullOrWhiteSpace();

        JsonDocument.Parse(detail.PlanSnapshotJson);
    }

    private async Task<Guid> SeedMinimalPlanInDefaultScopeAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        IProductLearningPlanningRepository planning = scope.ServiceProvider.GetRequiredService<IProductLearningPlanningRepository>();

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
                DerivationRuleVersion = "test",
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
                        Ordinal = 1,
                        ActionType = "Observe",
                        Description = "Observe only.",
                    },
                ],
            },
            CancellationToken.None);

        return planId;
    }
}
