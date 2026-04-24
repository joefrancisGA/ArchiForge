using System.Text.Json;

using ArchLucid.Core.Integration;
using ArchLucid.Core.Notifications.Teams;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Notifications.Teams;

public sealed class TeamsNotificationTriggerCatalogTests
{
    [Fact]
    public void All_contains_v1_default_six_triggers()
    {
        TeamsNotificationTriggerCatalog.All.Should().HaveCount(6);
        TeamsNotificationTriggerCatalog.All.Should().Contain(IntegrationEventTypes.AuthorityRunCompletedV1);
        TeamsNotificationTriggerCatalog.All.Should().Contain(IntegrationEventTypes.GovernanceApprovalSubmittedV1);
        TeamsNotificationTriggerCatalog.All.Should().Contain(IntegrationEventTypes.AlertFiredV1);
        TeamsNotificationTriggerCatalog.All.Should().Contain(IntegrationEventTypes.ComplianceDriftEscalatedV1);
        TeamsNotificationTriggerCatalog.All.Should().Contain(IntegrationEventTypes.AdvisoryScanCompletedV1);
        TeamsNotificationTriggerCatalog.All.Should().Contain(IntegrationEventTypes.SeatReservationReleasedV1);
    }

    [Fact]
    public void IsKnown_returns_false_for_blank_or_unknown()
    {
        TeamsNotificationTriggerCatalog.IsKnown("").Should().BeFalse();
        TeamsNotificationTriggerCatalog.IsKnown("   ").Should().BeFalse();
        TeamsNotificationTriggerCatalog.IsKnown("com.archlucid.does.not.exist").Should().BeFalse();
    }

    [Fact]
    public void IsKnown_returns_true_for_every_catalog_entry()
    {
        foreach (string trigger in TeamsNotificationTriggerCatalog.All)
        {
            TeamsNotificationTriggerCatalog.IsKnown(trigger).Should().BeTrue($"{trigger} is in All");
        }
    }

    [Fact]
    public void DefaultEnabledTriggersJson_round_trips_through_ParseOrDefault_to_full_catalog()
    {
        IReadOnlyList<string> parsed =
            TeamsNotificationTriggerCatalog.ParseOrDefault(TeamsNotificationTriggerCatalog.DefaultEnabledTriggersJson);

        parsed.Should().BeEquivalentTo(TeamsNotificationTriggerCatalog.All);
    }

    [Fact]
    public void ParseOrDefault_returns_full_catalog_for_null_or_empty_or_invalid_json()
    {
        TeamsNotificationTriggerCatalog.ParseOrDefault(null).Should()
            .BeEquivalentTo(TeamsNotificationTriggerCatalog.All);
        TeamsNotificationTriggerCatalog.ParseOrDefault("").Should().BeEquivalentTo(TeamsNotificationTriggerCatalog.All);
        TeamsNotificationTriggerCatalog.ParseOrDefault("   ").Should()
            .BeEquivalentTo(TeamsNotificationTriggerCatalog.All);
        TeamsNotificationTriggerCatalog.ParseOrDefault("not json").Should()
            .BeEquivalentTo(TeamsNotificationTriggerCatalog.All);
    }

    [Fact]
    public void ParseOrDefault_drops_unknown_and_blank_entries()
    {
        string json = JsonSerializer.Serialize(new[]
        {
            IntegrationEventTypes.AuthorityRunCompletedV1, "", "com.archlucid.does.not.exist",
            IntegrationEventTypes.AlertFiredV1, IntegrationEventTypes.AuthorityRunCompletedV1
        });

        IReadOnlyList<string> parsed = TeamsNotificationTriggerCatalog.ParseOrDefault(json);

        parsed.Should().BeEquivalentTo(IntegrationEventTypes.AuthorityRunCompletedV1,
            IntegrationEventTypes.AlertFiredV1);
    }

    [Fact]
    public void Serialize_orders_known_subset_canonically()
    {
        string serialized = TeamsNotificationTriggerCatalog.Serialize([
            IntegrationEventTypes.AlertFiredV1,
            IntegrationEventTypes.AuthorityRunCompletedV1
        ]);

        string[] parsed = JsonSerializer.Deserialize<string[]>(serialized)!;
        parsed.Should().Equal(
            IntegrationEventTypes.AuthorityRunCompletedV1,
            IntegrationEventTypes.AlertFiredV1);
    }

    [Fact]
    public void Serialize_returns_default_for_null_or_all_unknown_input()
    {
        TeamsNotificationTriggerCatalog.Serialize(null)
            .Should().Be(TeamsNotificationTriggerCatalog.DefaultEnabledTriggersJson);

        TeamsNotificationTriggerCatalog.Serialize(["unknown.one", "unknown.two"])
            .Should().Be(TeamsNotificationTriggerCatalog.DefaultEnabledTriggersJson);
    }

    [Fact]
    public void Unknown_returns_only_unknown_distinct_entries()
    {
        IReadOnlyList<string> unknown = TeamsNotificationTriggerCatalog.Unknown([
            IntegrationEventTypes.AuthorityRunCompletedV1,
            "com.archlucid.bad.one",
            "com.archlucid.bad.one",
            "",
            "com.archlucid.bad.two"
        ]);

        unknown.Should().BeEquivalentTo("com.archlucid.bad.one", "com.archlucid.bad.two");
    }

    [Fact]
    public void Unknown_returns_empty_for_null()
    {
        TeamsNotificationTriggerCatalog.Unknown(null).Should().BeEmpty();
    }
}
