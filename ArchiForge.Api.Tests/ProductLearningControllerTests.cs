using System.Net;
using System.Net.Http.Json;

using ArchiForge.Api.ProblemDetails;
using ArchiForge.Contracts.ProductLearning;

using FluentAssertions;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchiForge.Api.Tests;

/// <summary>Integration tests for <c>/v1/product-learning/*</c> (scoped read model; empty data is valid).</summary>
[Trait("Category", "Integration")]
public sealed class ProductLearningControllerTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetSummary_ReturnsOk_WithCountsAndNotes()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/product-learning/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ProductLearningDashboardSummaryResponse? body =
            await response.Content.ReadFromJsonAsync<ProductLearningDashboardSummaryResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.SummaryNotes.Should().NotBeEmpty();
        body.TotalSignalsInScope.Should().Be(0);
        body.TopAggregateCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSummary_InvalidSince_Returns400Problem()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/product-learning/summary?since=not-a-date");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.ValidationFailed);
        problem.Detail.Should().Contain("since");
    }

    [Fact]
    public async Task GetImprovementOpportunities_InvalidMax_Returns400Problem()
    {
        HttpResponseMessage response =
            await Client.GetAsync("/v1/product-learning/improvement-opportunities?maxOpportunities=999");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ProblemTypes.ValidationFailed);
        problem.Detail.Should().Contain("maxOpportunities");
    }

    [Fact]
    public async Task GetArtifactOutcomeTrends_ValidSince_ReturnsOk()
    {
        HttpResponseMessage response =
            await Client.GetAsync("/v1/product-learning/artifact-outcome-trends?since=2024-01-01T00:00:00Z");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ProductLearningArtifactOutcomeTrendsResponse? body =
            await response.Content.ReadFromJsonAsync<ProductLearningArtifactOutcomeTrendsResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Trends.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTriageQueue_Default_ReturnsOk()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/product-learning/triage-queue");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ProductLearningTriageQueueResponse? body =
            await response.Content.ReadFromJsonAsync<ProductLearningTriageQueueResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReport_FormatJson_ReturnsStructuredDocument()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/product-learning/report?format=json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ProductLearningTriageReportDocument? doc =
            await response.Content.ReadFromJsonAsync<ProductLearningTriageReportDocument>(JsonOptions);

        doc.Should().NotBeNull();
        doc!.TopProblemAreas.Should().NotBeNull();
        doc.ArtifactOutcomes.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReport_FormatMarkdown_ReturnsExportWrapper()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/product-learning/report?format=markdown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ProductLearningReportExportResponse? body =
            await response.Content.ReadFromJsonAsync<ProductLearningReportExportResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Format.Should().Be("markdown");
        body.Content.Should().Contain("Pilot feedback");
        body.FileName.Should().EndWith(".md");
    }

    [Fact]
    public async Task GetReport_InvalidFormat_Returns400()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/product-learning/report?format=xml");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReportFile_Markdown_ReturnsMarkdownContentType()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/product-learning/report/file?format=markdown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/markdown");
        string text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("# Pilot feedback");
    }
}
