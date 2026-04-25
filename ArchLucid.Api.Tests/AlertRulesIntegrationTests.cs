using System.Net;

using ArchLucid.Api.Routing;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Integration tests: Alert Rules (HTTP host, database, or cross-component).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AlertRulesIntegrationTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListAlertRules_Returns200()
    {
        HttpResponseMessage response = await Client.GetAsync($"/{ApiV1Routes.AlertRules}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListAlerts_Returns200()
    {
        HttpResponseMessage response = await Client.GetAsync($"/{ApiV1Routes.Alerts}?take=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
