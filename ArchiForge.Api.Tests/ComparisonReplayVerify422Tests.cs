using System.Net;
using System.Text;
using System.Text.Json;

using ArchiForge.Api.Services;
using ArchiForge.Application.Analysis;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchiForge.Api.Tests;

/// <summary>Replaces comparison replay with a service that always fails verify, to assert HTTP 422 pipeline.</summary>
public sealed class ComparisonVerify422ApiFactory : ArchiForgeApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IComparisonReplayApiService>();
            services.AddScoped<IComparisonReplayApiService, ForcedComparisonVerificationFailureService>();
        });
    }
}

internal sealed class ForcedComparisonVerificationFailureService : IComparisonReplayApiService
{
    public Task<ReplayComparisonResult> ReplayAsync(
        ReplayComparisonRequest request,
        bool metadataOnly,
        CancellationToken cancellationToken = default)
    {
        throw new ComparisonVerificationFailedException(
            "Regenerated comparison does not match stored payload.",
            new DriftAnalysisResult { DriftDetected = true, Summary = "integration-test drift" });
    }

    public Task<DriftAnalysisResult> AnalyzeDriftAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new DriftAnalysisResult());
}

[Trait("Category", "Integration")]
public sealed class ComparisonReplayVerify422Tests(ComparisonVerify422ApiFactory factory)
    : IClassFixture<ComparisonVerify422ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ReplayComparison_WhenVerificationFails_Returns422ProblemDetailsWithDrift()
    {
        string body = JsonSerializer.Serialize(new
        {
            format = "markdown",
            replayMode = "verify",
            persistReplay = false
        });
        HttpResponseMessage response = await _client.PostAsync(
            "/v1/architecture/comparisons/does-not-matter/replay",
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(422);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Unprocessable Entity");
        doc.RootElement.GetProperty("type").GetString().Should().Contain("comparison-verification-failed");
        doc.RootElement.GetProperty("driftDetected").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("driftSummary").GetString().Should().Be("integration-test drift");
    }
}
