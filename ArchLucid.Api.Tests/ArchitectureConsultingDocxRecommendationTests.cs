using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Consulting Docx Recommendation.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureConsultingDocxRecommendationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task RecommendProfile_ForExecutiveAudience_ReturnsExecutive()
    {
        var request = new
        {
            audience = "Executives and sponsors",
            externalDelivery = true,
            executiveFriendly = true,
            regulatedEnvironment = false,
            needDetailedEvidence = false,
            needExecutionTraces = false,
            needDeterminismOrCompareAppendices = false
        };

        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/analysis-report/export/docx/consulting/profiles/recommend",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ConsultingDocxProfileRecommendationResponse? payload =
            await response.Content.ReadFromJsonAsync<ConsultingDocxProfileRecommendationResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Recommendation.RecommendedProfileName.Should().Be("executive");
        payload.Recommendation.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RecommendProfile_ForRegulatedReview_ReturnsRegulated()
    {
        var request = new
        {
            audience = "Compliance and audit reviewers",
            externalDelivery = true,
            executiveFriendly = false,
            regulatedEnvironment = true,
            needDetailedEvidence = true,
            needExecutionTraces = true,
            needDeterminismOrCompareAppendices = true
        };

        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/analysis-report/export/docx/consulting/profiles/recommend",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ConsultingDocxProfileRecommendationResponse? payload =
            await response.Content.ReadFromJsonAsync<ConsultingDocxProfileRecommendationResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Recommendation.RecommendedProfileName.Should().Be("regulated");
    }
}
