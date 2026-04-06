namespace ArchiForge.Decisioning.Advisory.Delivery;

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken ct);
}
