using ArchLucid.Decisioning.Alerts.Delivery;

using FluentAssertions;

using FsCheck.Xunit;

namespace ArchLucid.Decisioning.Tests.Alerts.Delivery;

/// <summary>
/// Stable composite identity hints for delivery audit rows (dispatcher stores AlertId + subscription + channel snapshot).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AlertDeliveryCompositeKeyPropertyTests
{
    [Property(MaxTest = 200)]
    public void Composite_route_key_distinguishes_alerts_and_subscriptions(
        Guid alertIdA,
        Guid alertIdB,
        Guid subscriptionIdA,
        Guid subscriptionIdB,
        string channel,
        string destination)
    {
        if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(destination))
            return;

        if (alertIdA == alertIdB && subscriptionIdA == subscriptionIdB)
            return;

        string keyA = BuildCompositeRouteKey(alertIdA, subscriptionIdA, channel, destination);
        string keyB = BuildCompositeRouteKey(alertIdB, subscriptionIdB, channel, destination);

        keyA.Should().NotBe(keyB);
    }

    [Property(MaxTest = 100)]
    public void Same_inputs_yield_same_composite_key(
        Guid alertId,
        Guid subscriptionId,
        string channel,
        string destination)
    {
        if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(destination))
            return;

        BuildCompositeRouteKey(alertId, subscriptionId, channel, destination)
            .Should()
            .Be(BuildCompositeRouteKey(alertId, subscriptionId, channel, destination));
    }

    [Fact]
    public void AlertDeliveryAttempt_defaults_use_Started_status()
    {
        AlertDeliveryAttempt attempt = new();

        attempt.Status.Should().Be(AlertDeliveryAttemptStatus.Started);
    }

    private static string BuildCompositeRouteKey(
        Guid alertId,
        Guid subscriptionId,
        string channelType,
        string destination) =>
        $"{alertId:N}|{subscriptionId:N}|{channelType}|{destination}";
}
