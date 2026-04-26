using System.Collections.Frozen;
using System.Text.Json;

using ArchLucid.Core.Integration;

namespace ArchLucid.Core.Notifications.Teams;

/// <summary>
///     Canonical catalog of integration event types eligible to fan out to Microsoft Teams via the
///     per-tenant `dbo.TenantTeamsIncomingWebhookConnections.EnabledTriggersJson` opt-in column.
/// </summary>
/// <remarks>
///     Single source of truth for the v1 trigger set defined in PENDING_QUESTIONS.md item 32 + 23 sub-bullet
///     (Resolved 2026-04-21). The Logic Apps workflow filters events server-side against this list before
///     fan-out so a disabled trigger cannot reach Teams even if upstream routing misbehaves. The HTTP API
///     validates incoming opt-in lists are a subset of <see cref="All" /> via <see cref="IsKnown" />.
/// </remarks>
public static class TeamsNotificationTriggerCatalog
{
    /// <summary>v1 default trigger set (all-on for fresh tenants and existing rows).</summary>
    public static readonly IReadOnlyList<string> All =
    [
        IntegrationEventTypes.AuthorityRunCompletedV1,
        IntegrationEventTypes.GovernanceApprovalSubmittedV1,
        IntegrationEventTypes.AlertFiredV1,
        IntegrationEventTypes.ComplianceDriftEscalatedV1,
        IntegrationEventTypes.AdvisoryScanCompletedV1,
        IntegrationEventTypes.SeatReservationReleasedV1
    ];

    /// <summary>O(1) membership test backing <see cref="IsKnown" />.</summary>
    private static readonly FrozenSet<string> AllSet =
        All.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    ///     Default JSON payload for the SQL column when a tenant has not explicitly chosen \u2014 mirrors the migration
    ///     default in 107.
    /// </summary>
    public static readonly string DefaultEnabledTriggersJson =
        JsonSerializer.Serialize(All);

    /// <summary>True when <paramref name="eventType" /> is one of the v1 catalog triggers.</summary>
    public static bool IsKnown(string eventType)
    {
        return !string.IsNullOrWhiteSpace(eventType) && AllSet.Contains(eventType);
    }

    /// <summary>
    ///     Deserialize <paramref name="json" /> into a deduplicated list of catalog triggers, dropping unknown / blank
    ///     entries.
    /// </summary>
    /// <remarks>
    ///     Returns the v1 default <see cref="All" /> when the column is empty / missing / not a JSON array. This is the safest
    ///     "no opinion" interpretation for legacy rows that pre-dated migration 107 and lets the Logic Apps filter still fan
    ///     every v1 event out without a database backfill.
    /// </remarks>
    public static IReadOnlyList<string> ParseOrDefault(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return All;

        try
        {
            string[]? parsed = JsonSerializer.Deserialize<string[]>(json);

            if (parsed is null || parsed.Length == 0)
                return All;

            return parsed
                .Where(IsKnown)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
        catch (JsonException)
        {
            return All;
        }
    }

    /// <summary>
    ///     Serialize <paramref name="enabledTriggers" /> after filtering to known catalog entries; deterministic ordering
    ///     matches <see cref="All" />.
    /// </summary>
    public static string Serialize(IEnumerable<string>? enabledTriggers)
    {
        if (enabledTriggers is null)
            return DefaultEnabledTriggersJson;

        FrozenSet<string> requested = enabledTriggers
            .Where(IsKnown)
            .ToFrozenSet(StringComparer.Ordinal);

        if (requested.Count == 0)
            return DefaultEnabledTriggersJson;

        string[] ordered = All.Where(requested.Contains).ToArray();
        return JsonSerializer.Serialize(ordered);
    }

    /// <summary>Returns the unknown trigger names in <paramref name="enabledTriggers" /> for HTTP 400 reporting.</summary>
    public static IReadOnlyList<string> Unknown(IEnumerable<string>? enabledTriggers)
    {
        if (enabledTriggers is null)
            return [];

        return enabledTriggers
            .Where(t => !string.IsNullOrWhiteSpace(t) && !IsKnown(t))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
