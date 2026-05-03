using System.Net;
using System.Text;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Authorization + validation branches on <see cref="ArchLucid.Api.Controllers.Advisory.AdvisorySchedulingController" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class AdvisorySchedulingControllerIntegrationTests
{
    [SkippableFact]
    public async Task ListSchedules_anonymous_returns_unauthorized()
    {
        await using HealthEndpointSecurityApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/advisory-scheduling/schedules");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task CreateSchedule_reader_returns_forbidden()
    {
        await using ReaderRoleArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using StringContent body = new(
            """{"name":"sched-int-test","cronExpression":"0 7 * * *","isEnabled":true}""",
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await client.PostAsync("/v1/advisory-scheduling/schedules", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task CreateSchedule_admin_null_body_returns_bad_request()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using StringContent body = new("null", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/v1/advisory-scheduling/schedules", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task ListSchedules_returns_ok_and_empty_when_none()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/advisory-scheduling/schedules");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string json = (await response.Content.ReadAsStringAsync()).Trim();
        json.Should().Be("[]");
    }

    [SkippableFact]
    public async Task ListExecutions_unknown_schedule_returns_not_found()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        Guid missing = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        HttpResponseMessage response =
            await client.GetAsync($"/v1/advisory-scheduling/schedules/{missing:D}/executions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task GetDigest_unknown_returns_not_found()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        Guid missing = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        HttpResponseMessage response = await client.GetAsync($"/v1/advisory-scheduling/digests/{missing:D}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
