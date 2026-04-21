using ArchLucid.Application.Bootstrap;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Bootstrap;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ContosoRetailDemoIdentifiersMatcherTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("not-a-demo-run", false)]
    public void IsDemoRunId_rejects_unknown_runs(string? runId, bool expected) =>
        ContosoRetailDemoIdentifiers.IsDemoRunId(runId).Should().Be(expected);

    [Fact]
    public void IsDemoRunId_matches_canonical_baseline_run()
        => ContosoRetailDemoIdentifiers.IsDemoRunId(ContosoRetailDemoIdentifiers.RunBaseline).Should().BeTrue();

    [Fact]
    public void IsDemoRunId_matches_canonical_hardened_run()
        => ContosoRetailDemoIdentifiers.IsDemoRunId(ContosoRetailDemoIdentifiers.RunHardened).Should().BeTrue();

    [Fact]
    public void IsDemoRunId_matches_case_insensitively()
        => ContosoRetailDemoIdentifiers.IsDemoRunId(ContosoRetailDemoIdentifiers.RunBaseline.ToUpperInvariant()).Should().BeTrue();

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("req-other", false)]
    public void IsDemoRequestId_rejects_unknown_requests(string? requestId, bool expected) =>
        ContosoRetailDemoIdentifiers.IsDemoRequestId(requestId).Should().Be(expected);

    [Fact]
    public void IsDemoRequestId_matches_canonical_request()
        => ContosoRetailDemoIdentifiers.IsDemoRequestId(ContosoRetailDemoIdentifiers.RequestContoso).Should().BeTrue();

    [Fact]
    public void IsDemoRequestId_matches_multi_tenant_prefix_with_suffix()
        => ContosoRetailDemoIdentifiers.IsDemoRequestId("req-contoso-demo-abc123def456").Should().BeTrue();
}
