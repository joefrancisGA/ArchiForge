namespace ArchLucid.Persistence.Models;

internal sealed class TenantExecDigestPreferencesRow
{
    public Guid TenantId
    {
        get;
        init;
    }

    public int SchemaVersion
    {
        get;
        init;
    }

    public bool EmailEnabled
    {
        get;
        init;
    }

    public string? RecipientEmails
    {
        get;
        init;
    }

    public string IanaTimeZoneId
    {
        get;
        init;
    } = "UTC";

    public byte DayOfWeek
    {
        get;
        init;
    }

    public byte HourOfDay
    {
        get;
        init;
    }

    public DateTime UpdatedUtc
    {
        get;
        init;
    }
}
