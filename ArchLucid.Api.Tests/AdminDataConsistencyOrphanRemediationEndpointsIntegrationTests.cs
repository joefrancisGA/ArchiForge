using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Smoke tests for admin data-consistency orphan remediation endpoints (InMemory storage returns empty sets).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class AdminDataConsistencyOrphanRemediationEndpointsIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Post_orphan_golden_manifests_dry_run_returns_ok_with_zero_rows()
    {
        HttpResponseMessage response =
            await Client.PostAsync("/v1/admin/diagnostics/data-consistency/orphan-golden-manifests?dryRun=true&maxRows=10", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        OrphanGoldenManifestRemediationResultDto? body =
            await response.Content.ReadFromJsonAsync<OrphanGoldenManifestRemediationResultDto>(JsonOptions);

        body.Should().NotBeNull();
        body!.DryRun.Should().BeTrue();
        body.RowCount.Should().Be(0);
        body.ManifestIds.Should().NotBeNull();
        body.ManifestIds!.Count.Should().Be(0);
    }

    [Fact]
    public async Task Post_orphan_findings_snapshots_dry_run_returns_ok_with_zero_rows()
    {
        HttpResponseMessage response =
            await Client.PostAsync("/v1/admin/diagnostics/data-consistency/orphan-findings-snapshots?dryRun=true&maxRows=10", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        OrphanFindingsSnapshotRemediationResultDto? body =
            await response.Content.ReadFromJsonAsync<OrphanFindingsSnapshotRemediationResultDto>(JsonOptions);

        body.Should().NotBeNull();
        body!.DryRun.Should().BeTrue();
        body.RowCount.Should().Be(0);
        body.FindingsSnapshotIds.Should().NotBeNull();
        body.FindingsSnapshotIds!.Count.Should().Be(0);
    }

    private sealed record OrphanGoldenManifestRemediationResultDto(
        bool DryRun,
        int RowCount,
        IReadOnlyList<string>? ManifestIds);

    private sealed record OrphanFindingsSnapshotRemediationResultDto(
        bool DryRun,
        int RowCount,
        IReadOnlyList<string>? FindingsSnapshotIds);
}
