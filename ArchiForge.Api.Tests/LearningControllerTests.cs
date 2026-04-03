using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchiForge.Api.Models.Learning;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;

using FluentAssertions;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchiForge.Api.Tests;

/// <summary>Integration tests for <c>/v1/learning/*</c> (59R planning read model).</summary>
[Trait("Category", "Integration")]
[Trait("ChangeSet", "59R")]
public sealed class LearningControllerTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetThemes_Default_ReturnsOk_WithEmptyThemes()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/themes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        LearningThemesListResponse? body = await response.Content.ReadFromJsonAsync<LearningThemesListResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Themes.Should().NotBeNull();
        body.Themes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetThemes_InvalidMax_Returns400Problem()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/themes?maxThemes=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.ValidationFailed);
        problem.Detail.Should().Contain("maxThemes");
    }

    [Fact]
    public async Task GetPlans_Default_ReturnsOk_WithEmptyPlans()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        LearningPlansListResponse? body = await response.Content.ReadFromJsonAsync<LearningPlansListResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Plans.Should().NotBeNull();
        body.Plans.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlanById_InvalidGuid_Returns400Problem()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/plans/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public async Task GetPlanById_UnknownGuid_Returns404Problem()
    {
        HttpResponseMessage response =
            await Client.GetAsync("/v1/learning/plans/00000000-0000-0000-0000-000000000001");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.LearningImprovementPlanNotFound);
    }

    [Fact]
    public async Task GetSummary_Default_ReturnsOk_WithZeroCounts()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        LearningSummaryResponse? body = await response.Content.ReadFromJsonAsync<LearningSummaryResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.ThemeCount.Should().Be(0);
        body.PlanCount.Should().Be(0);
        body.TotalThemeEvidenceSignals.Should().Be(0);
        body.TotalLinkedSignalsAcrossPlans.Should().Be(0);
        body.MaxPlanPriorityScore.Should().BeNull();
    }

    [Fact]
    public async Task GetSummary_InvalidMaxPlans_Returns400Problem()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/summary?maxPlans=abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Detail.Should().Contain("maxPlans");
    }

    [Fact]
    public async Task GetPlanningReport_Json_ReturnsOk_WithDocumentShape()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/report?format=json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        LearningPlanningReportDocument? doc =
            await response.Content.ReadFromJsonAsync<LearningPlanningReportDocument>(JsonOptions);

        doc.Should().NotBeNull();
        doc!.Summary.ThemeCount.Should().Be(0);
        doc.Themes.Should().BeEmpty();
        doc.Plans.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlanningReport_Markdown_ReturnsOk_WithWrapper()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/report?format=markdown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        LearningPlanningReportExportResponse? body =
            await response.Content.ReadFromJsonAsync<LearningPlanningReportExportResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Format.Should().Be("markdown");
        body.FileName.Should().Be("learning-planning-report-59r.md");
        body.Content.Should().Contain("ArchiForge planning report (59R)");
    }

    [Fact]
    public async Task GetPlanningReport_InvalidFormat_Returns400Problem()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/report?format=xml");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public async Task DownloadPlanningReport_File_Markdown_ReturnsMarkdownContentType()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/report/file?format=markdown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/markdown");

        string text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("ArchiForge planning report (59R)");
    }

    [Fact]
    public async Task DownloadPlanningReport_File_Json_IsIndentedObject()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/learning/report/file?format=json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        string text = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(text);
        doc.RootElement.GetProperty("summary").GetProperty("themeCount").GetInt32().Should().Be(0);
    }
}
