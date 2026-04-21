using System.Net.Http.Json;

using ArchLucid.Api.Filters;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Integration test for ADR 0021 Phase 2 — confirms the coordinator-pipeline routes (currently mounted on
/// <c>/v1/architecture/...</c> via <c>RunsController</c>) emit the standards-track deprecation triplet
/// (<c>Deprecation</c> / <c>Sunset</c> / <c>Link</c>) on real HTTP responses, and that adjacent
/// non-coordinator routes (the read-only <c>RunQueryController</c>) do <em>not</em> — only the routes
/// that are actually being retired carry the signal.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class CoordinatorRoutesDeprecationHeaderTests : IntegrationTestBase
{
    public CoordinatorRoutesDeprecationHeaderTests(ArchLucidApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Coordinator_create_run_route_emits_RFC_9745_deprecation_triplet()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/v1/architecture/request",
            TestRequestFactory.CreateArchitectureRequest("REQ-DEPR-HDR-" + Guid.NewGuid().ToString("N")[..8]));

        AssertDeprecationTripletPresent(response);
    }

    [Fact]
    public async Task Coordinator_commit_route_emits_deprecation_triplet_even_on_404()
    {
        // RFC 9745 §3 — a deprecated resource MUST advertise its deprecation on every applicable
        // response, including problem-details responses. Hitting a non-existent run id exercises the
        // 404 branch of the action and we still expect the headers.
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/run/nonexistent-run-id/commit",
            content: null);

        AssertDeprecationTripletPresent(response);
    }

    [Fact]
    public async Task Read_only_RunQueryController_route_does_not_emit_deprecation_headers()
    {
        // RunQueryController is the read-only sibling and reads through IUnifiedGoldenManifestReader —
        // it is part of the *unified* read path and is NOT being retired. The route hitting a missing
        // run returns 404 (problem-details) — we just need to assert the deprecation headers are absent.
        HttpResponseMessage response = await Client.GetAsync("/v1/architecture/run/nonexistent-run-id");

        response.Headers.Contains("Deprecation").Should().BeFalse(
            because: "RunQueryController is part of the unified Authority read path, not the deprecated coordinator pipeline");
        response.Headers.Contains("Sunset").Should().BeFalse();
    }

    private static void AssertDeprecationTripletPresent(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Deprecation", out IEnumerable<string>? deprecationValues).Should().BeTrue(
            because: "ADR 0021 Phase 2 requires every coordinator route response to carry RFC 9745 Deprecation");
        deprecationValues!.Should().ContainSingle().Which.Should().Be("true");

        response.Headers.TryGetValues("Sunset", out IEnumerable<string>? sunsetValues).Should().BeTrue(
            because: "ADR 0021 Phase 2 requires the RFC 8594 Sunset signal alongside Deprecation");
        sunsetValues!.Should().ContainSingle().Which.Should().Be(CoordinatorPipelineDeprecationFilter.SunsetHttpDate);

        response.Headers.TryGetValues("Link", out IEnumerable<string>? linkValues).Should().BeTrue(
            because: "ADR 0021 Phase 2 requires an RFC 8288 Link header pointing at the migration target ADR");
        linkValues!.Should().Contain(value => value.Contains("0021-coordinator-pipeline-strangler-plan.md", StringComparison.Ordinal));
    }
}
