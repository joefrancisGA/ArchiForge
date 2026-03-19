using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureConsultingDocxRecommendationTests(ArchiForgeApiFactory factory)
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

        var response = await Client.PostAsync(
            "/v1/architecture/analysis-report/export/docx/consulting/profiles/recommend",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ConsultingDocxProfileRecommendationResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Recommendation.RecommendedProfileName.Should().Be("executive");
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

        var response = await Client.PostAsync(
            "/v1/architecture/analysis-report/export/docx/consulting/profiles/recommend",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ConsultingDocxProfileRecommendationResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Recommendation.RecommendedProfileName.Should().Be("regulated");
    }
}

