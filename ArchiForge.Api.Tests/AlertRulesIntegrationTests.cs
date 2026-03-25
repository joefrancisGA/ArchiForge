using System.Net;

using ArchiForge.Api.Routing;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class AlertRulesIntegrationTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
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
