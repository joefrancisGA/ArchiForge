using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Routing;
using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Decisioning.Alerts;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     End-to-end: create simple alert rule → run advisory scan (evaluates rules) → list persisted
///     <see cref="AlertRecord" /> rows via HTTP.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AlertLifecycleIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Create_rule_run_advisory_scan_list_alerts_persists_open_alert()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        await AdvisoryIntegrationSeed.SeedDefaultScopeAuthorityRunAsync(factory.Services, CancellationToken.None)
            ;

        HttpClient client = factory.CreateClient();

        HttpResponseMessage createRuleResponse = await client.PostAsJsonAsync(
            $"/{ApiV1Routes.AlertRules}",
            new
            {
                name = "Integration lifecycle — critical rec count",
                ruleType = AlertRuleType.CriticalRecommendationCount,
                severity = AlertSeverity.Warning,
                thresholdValue = 0m,
                isEnabled = true,
                targetChannelType = "DigestOnly"
            },
            JsonOptions,
            CancellationToken.None);

        createRuleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AlertRule? createdRule =
                await createRuleResponse.Content.ReadFromJsonAsync<AlertRule>(JsonOptions, CancellationToken.None)
            ;
        createdRule.Should().NotBeNull();
        Guid ruleId = createdRule.RuleId;
        ruleId.Should().NotBeEmpty();

        HttpResponseMessage createScheduleResponse = await client.PostAsJsonAsync(
            "v1/advisory-scheduling/schedules",
            new
            {
                name = "Lifecycle test scan",
                cronExpression = "0 7 * * *",
                isEnabled = true,
                runProjectSlug = AdvisoryScanSchedule.DefaultProjectSlug
            },
            JsonOptions,
            CancellationToken.None);

        createScheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AdvisoryScanSchedule? schedule = await createScheduleResponse.Content
                .ReadFromJsonAsync<AdvisoryScanSchedule>(JsonOptions, CancellationToken.None)
            ;
        schedule.Should().NotBeNull();

        HttpResponseMessage runResponse = await client
                .PostAsync($"v1/advisory-scheduling/schedules/{schedule.ScheduleId:D}/run", null,
                    CancellationToken.None)
            ;

        runResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage listAlertsResponse = await client
                .GetAsync(new Uri($"/{ApiV1Routes.Alerts}?take=50", UriKind.Relative), CancellationToken.None)
            ;

        listAlertsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<AlertRecord>? alerts = await listAlertsResponse.Content
                .ReadFromJsonAsync<List<AlertRecord>>(JsonOptions, CancellationToken.None)
            ;

        alerts.Should().NotBeNull();
        alerts.Should().Contain(a =>
            a.RuleId == ruleId && string.Equals(a.Status, AlertStatus.Open, StringComparison.OrdinalIgnoreCase));
    }
}
