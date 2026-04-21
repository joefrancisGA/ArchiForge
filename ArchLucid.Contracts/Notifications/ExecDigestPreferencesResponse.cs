namespace ArchLucid.Contracts.Notifications;

/// <summary>Operator-configured weekly executive digest email preferences for the current tenant.</summary>
public sealed class ExecDigestPreferencesResponse
{
    public int SchemaVersion { get; init; } = 1;

    public Guid TenantId { get; init; }

    /// <summary><see langword="false"/> until a row exists in SQL (GET still returns defaults).</summary>
    public bool IsConfigured { get; init; }

    public bool EmailEnabled { get; init; }

    public IReadOnlyList<string> RecipientEmails { get; init; } = [];

    public string IanaTimeZoneId { get; init; } = "UTC";

    /// <summary><see cref="System.DayOfWeek"/> value (0 = Sunday … 6 = Saturday).</summary>
    public int DayOfWeek { get; init; } = 1;

    public int HourOfDay { get; init; } = 8;

    public DateTimeOffset UpdatedUtc { get; init; }

    public static ExecDigestPreferencesResponse Unconfigured(Guid tenantId) => new()
    {
        TenantId = tenantId,
        IsConfigured = false,
        EmailEnabled = false,
        RecipientEmails = [],
        IanaTimeZoneId = "UTC",
        DayOfWeek = 1,
        HourOfDay = 8,
        UpdatedUtc = DateTimeOffset.UtcNow,
    };
}
