using System.Net;

using ArchLucid.Api.Tests;
using ArchLucid.Core.Scoping;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Planning;

/// <summary>
///     Covers authenticated Planning (<see cref="ArchLucid.Api.Controllers.Planning.GraphController" />) and Governance
///     (<see cref="ArchLucid.Api.Controllers.Governance.GovernanceController" />) commercial-tier gated routes â€”
///     complements <see cref="CommercialTenantTierFilterTests" /> against the default DevelopmentBypass scoped host.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class PlanningGovernanceCommercialSmokeIntegrationTests : IClassFixture<ArchLucidApiFactory>
{
    private readonly ArchLucidApiFactory _factory;

    public PlanningGovernanceCommercialSmokeIntegrationTests(ArchLucidApiFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Get_graph_run_returns_404_when_no_snapshot_exists()
    {
        HttpClient client = _factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        Guid fakeRunId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        using HttpResponseMessage response =
            await client.GetAsync($"/v1/graph/runs/{fakeRunId:D}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task Get_governance_dashboard_returns_200_when_tenant_meets_standard_tier()
    {
        HttpClient client = _factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using HttpResponseMessage response =
            await client.GetAsync("/v1/governance/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
