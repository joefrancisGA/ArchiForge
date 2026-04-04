using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchiForge.Api.Routing;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Scheduling;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

/// <summary>
/// End-to-end: create digest subscription → advisory scan persists digest and dispatcher records a delivery attempt → GET attempts.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DigestDeliveryLifecycleIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Create_subscription_run_advisory_scan_lists_succeeded_delivery_attempt()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        await AdvisoryIntegrationSeed.SeedDefaultScopeAuthorityRunAsync(factory.Services, CancellationToken.None)
;

        HttpClient client = factory.CreateClient();

        HttpResponseMessage subResponse = await client.PostAsJsonAsync(
            $"/{ApiV1Routes.DigestSubscriptions}",
            new
            {
                name = "Lifecycle digest email",
                channelType = DigestDeliveryChannelType.Email,
                destination = "operator@example.com",
                isEnabled = true
            },
            JsonOptions,
            CancellationToken.None);

        subResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        DigestSubscription? subscription = await subResponse.Content
            .ReadFromJsonAsync<DigestSubscription>(JsonOptions, CancellationToken.None)
;
        subscription.Should().NotBeNull();
        Guid subscriptionId = subscription.SubscriptionId;

        HttpResponseMessage createScheduleResponse = await client.PostAsJsonAsync(
            "v1/advisory-scheduling/schedules",
            new
            {
                name = "Digest lifecycle scan",
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
            .PostAsync($"v1/advisory-scheduling/schedules/{schedule.ScheduleId:D}/run", content: null, CancellationToken.None)
;

        runResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage attemptsResponse = await client
            .GetAsync(new Uri($"/{ApiV1Routes.DigestSubscriptions}/{subscriptionId:D}/attempts?take=20", UriKind.Relative), CancellationToken.None)
;

        attemptsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<DigestDeliveryAttempt>? attempts = await attemptsResponse.Content
            .ReadFromJsonAsync<List<DigestDeliveryAttempt>>(JsonOptions, CancellationToken.None)
;

        attempts.Should().NotBeNull();
        attempts.Should().Contain(a =>
            a.SubscriptionId == subscriptionId &&
            string.Equals(a.Status, DigestDeliveryStatus.Succeeded, StringComparison.OrdinalIgnoreCase));
    }
}
