using System.Net;
using System.Net.Http.Json;

using ArchLucid.Application.Bootstrap;
using ArchLucid.Contracts.Pilots;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>GET /v1/pilots/why-archlucid-snapshot</c> — the read-only telemetry projection
///     that powers the operator-shell <c>/why-archlucid</c> Core Pilot proof page.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class WhyArchLucidSnapshotEndpointTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetWhyArchLucidSnapshot_returns_ok_with_canonical_demo_run_id_and_zero_baseline_counters()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/pilots/why-archlucid-snapshot");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        WhyArchLucidSnapshotResponse? snapshot =
            await response.Content.ReadFromJsonAsync<WhyArchLucidSnapshotResponse>(JsonOptions);

        snapshot.Should().NotBeNull();
        snapshot.DemoRunId.Should().Be(ContosoRetailDemoIdentifiers.RunBaseline);
        snapshot.GeneratedUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2));
        snapshot.RunsCreatedTotal.Should().BeGreaterThanOrEqualTo(0);
        snapshot.AuditRowCount.Should().BeGreaterThanOrEqualTo(0);
        snapshot.AuditRowCountTruncated.Should().BeFalse();
        snapshot.FindingsProducedBySeverity.Should().NotBeNull();
    }
}
