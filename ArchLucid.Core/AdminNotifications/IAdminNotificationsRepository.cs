namespace ArchLucid.Core.AdminNotifications;

public interface IAdminNotificationsRepository
{
    Task InsertAsync(string kind, string summary, string? dataJson, CancellationToken cancellationToken);
}
