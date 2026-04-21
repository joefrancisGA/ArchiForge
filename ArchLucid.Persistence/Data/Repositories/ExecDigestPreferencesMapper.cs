using ArchLucid.Contracts.Notifications;

namespace ArchLucid.Persistence.Data.Repositories;

internal static class ExecDigestPreferencesMapper
{
    internal static ExecDigestPreferencesResponse ToResponse(Models.TenantExecDigestPreferencesRow row)
    {
        if (row is null)
            throw new ArgumentNullException(nameof(row));

        return new ExecDigestPreferencesResponse
        {
            SchemaVersion = row.SchemaVersion,
            TenantId = row.TenantId,
            IsConfigured = true,
            EmailEnabled = row.EmailEnabled,
            RecipientEmails = ParseEmails(row.RecipientEmails),
            IanaTimeZoneId = string.IsNullOrWhiteSpace(row.IanaTimeZoneId) ? "UTC" : row.IanaTimeZoneId.Trim(),
            DayOfWeek = row.DayOfWeek,
            HourOfDay = row.HourOfDay,
            UpdatedUtc = new DateTimeOffset(row.UpdatedUtc, TimeSpan.Zero),
        };
    }

    internal static string SerializeEmails(IReadOnlyList<string> emails)
    {
        if (emails is null || emails.Count == 0)
            return string.Empty;

        IEnumerable<string> trimmed = emails
            .Where(static e => !string.IsNullOrWhiteSpace(e))
            .Select(static e => e.Trim());

        return string.Join(';', trimmed);
    }

    private static IReadOnlyList<string> ParseEmails(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}
