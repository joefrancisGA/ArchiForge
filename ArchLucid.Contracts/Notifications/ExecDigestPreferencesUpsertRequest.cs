namespace ArchLucid.Contracts.Notifications;

/// <summary>Upsert body for <c>POST /v1/tenant/exec-digest-preferences</c>.</summary>
public sealed class ExecDigestPreferencesUpsertRequest
{
    public bool EmailEnabled { get; init; }

    public IReadOnlyList<string>? RecipientEmails { get; init; }

    public string? IanaTimeZoneId { get; init; }

    public int? DayOfWeek { get; init; }

    public int? HourOfDay { get; init; }
}
