using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Models;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP integration tests for <c>GET .../comparisons/{id}/replay/cost-estimate</c>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ComparisonReplayCostEstimateIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    private readonly ArchLucidApiFactory _factory = factory;

    [Fact]
    public async Task CostEstimate_existing_record_returns_200_with_band()
    {
        string id = "cmp_cost_est_" + Guid.NewGuid().ToString("N");

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IComparisonRecordRepository repo = scope.ServiceProvider.GetRequiredService<IComparisonRecordRepository>();
            await repo.CreateAsync(
                new ComparisonRecord
                {
                    ComparisonRecordId = id,
                    ComparisonType = "end-to-end-replay",
                    LeftRunId = "L",
                    RightRunId = "R",
                    Format = "json+markdown",
                    PayloadJson = "{}",
                    CreatedUtc = DateTime.UtcNow
                },
                CancellationToken.None);
        }

        HttpResponseMessage response = await Client.GetAsync(
            $"/v1/architecture/comparisons/{id}/replay/cost-estimate?replayMode=artifact&format=markdown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ComparisonReplayCostEstimateResponse? body =
            await response.Content.ReadFromJsonAsync<ComparisonReplayCostEstimateResponse>(JsonOptions);
        body.Should().NotBeNull();
        body.ComparisonRecordId.Should().Be(id);
        body.RelativeCostBand.Should().BeOneOf("low", "medium", "high");
        body.ReplayMode.Should().Be("artifact");
    }

    [Fact]
    public async Task CostEstimate_unknown_record_returns_404()
    {
        HttpResponseMessage response = await Client.GetAsync(
            "/v1/architecture/comparisons/nonexistent_cmp_xyz/replay/cost-estimate");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
