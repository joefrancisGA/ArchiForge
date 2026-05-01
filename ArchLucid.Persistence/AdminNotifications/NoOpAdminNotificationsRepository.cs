using ArchLucid.Core.AdminNotifications;

namespace ArchLucid.Persistence.AdminNotifications;

/// <summary>In-memory no-op implementation (SCIM rotation reminders are SQL-shaped; in-memory hosts skip inserts).</summary>
public sealed class NoOpAdminNotificationsRepository : IAdminNotificationsRepository
{
    /// <inheritdoc />
    public Task InsertAsync(string kind, string summary, string? dataJson, CancellationToken cancellationToken)
    {
        _ = kind;
        _ = summary;
        _ = dataJson;
        _ = cancellationToken;

        return Task.CompletedTask;
    }
}
