using System.Text;
using System.Text.Json;

using ArchLucid.Core.Integration;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Integration;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class IntegrationEventServiceBusApplicationPropertiesTests
{
    [Fact]
    public void TryResolveForPublish_governance_promotion_maps_environment_to_user_property()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(
                new
                {
                    schemaVersion = 1,
                    environment = "Prod",
                }));

        IReadOnlyDictionary<string, object>? props =
            IntegrationEventServiceBusApplicationProperties.TryResolveForPublish(
                IntegrationEventTypes.GovernancePromotionActivatedV1,
                utf8);

        props.Should().NotBeNull();
        props[IntegrationEventServiceBusApplicationProperties.PromotionEnvironmentPropertyName].Should().Be("prod");
    }

    [Fact]
    public void TryResolveForPublish_alert_fired_without_severity_or_dedupe_returns_null()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("{\"schemaVersion\":1}");

        IntegrationEventServiceBusApplicationProperties
            .TryResolveForPublish(IntegrationEventTypes.AlertFiredV1, utf8)
            .Should()
            .BeNull();
    }

    [Fact]
    public void TryResolveForPublish_alert_fired_maps_severity_and_deduplication_key()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(
                new
                {
                    schemaVersion = 1,
                    severity = "High",
                    deduplicationKey = "rule:1:run:a",
                }));

        IReadOnlyDictionary<string, object>? props =
            IntegrationEventServiceBusApplicationProperties.TryResolveForPublish(
                IntegrationEventTypes.AlertFiredV1,
                utf8);

        props.Should().NotBeNull();
        props[IntegrationEventServiceBusApplicationProperties.SeverityPropertyName].Should().Be("high");
        props[IntegrationEventServiceBusApplicationProperties.DeduplicationKeyPropertyName].Should().Be("rule:1:run:a");
    }

    [Fact]
    public void TryResolveForPublish_alert_resolved_maps_deduplication_key()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(
                new
                {
                    schemaVersion = 1,
                    deduplicationKey = " k ",
                }));

        IReadOnlyDictionary<string, object>? props =
            IntegrationEventServiceBusApplicationProperties.TryResolveForPublish(
                IntegrationEventTypes.AlertResolvedV1,
                utf8);

        props.Should().NotBeNull();
        props[IntegrationEventServiceBusApplicationProperties.DeduplicationKeyPropertyName].Should().Be("k");
    }
}
