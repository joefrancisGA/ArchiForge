using System.Net;
using System.Net.Mail;

using ArchLucid.Core.Configuration;
using ArchLucid.Core.Notifications.Email;

using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Notifications.Email;

/// <summary>Dev-oriented SMTP sender (legacy <see cref="SmtpClient" />; not recommended for production scale-out).</summary>
public sealed class SmtpEmailProvider(IOptionsMonitor<EmailNotificationOptions> optionsMonitor) : IEmailProvider
{
    private readonly IOptionsMonitor<EmailNotificationOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <inheritdoc />
    public string ProviderName => EmailProviderNames.Smtp;

    /// <inheritdoc />
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        EmailNotificationOptions options = _optionsMonitor.CurrentValue;

        if (string.IsNullOrWhiteSpace(options.SmtpHost))
            throw new InvalidOperationException("Email:SmtpHost is required when Email:Provider is Smtp.");


        if (string.IsNullOrWhiteSpace(options.FromAddress))
            throw new InvalidOperationException("Email:FromAddress is required when Email:Provider is Smtp.");


#pragma warning disable CA1416 // SmtpClient is obsolete but intentionally used for lightweight dev SMTP.
#pragma warning disable SYSLIB0014
        SmtpClient smtp = new(options.SmtpHost.Trim(), options.SmtpPort)
        {
            EnableSsl = options.SmtpPort is 587 or 465
        };

        if (!string.IsNullOrWhiteSpace(options.SmtpUser))
            smtp.Credentials = new NetworkCredential(options.SmtpUser.Trim(), options.SmtpPassword ?? string.Empty);

        MailAddress from = string.IsNullOrWhiteSpace(options.FromDisplayName)
            ? new MailAddress(options.FromAddress.Trim())
            : new MailAddress(options.FromAddress.Trim(), options.FromDisplayName.Trim());

        using (smtp)
        using (MailMessage mail = new(from, new MailAddress(message.To.Trim())))
        {
            mail.Subject = message.Subject;
            mail.Body = message.HtmlBody;
            mail.IsBodyHtml = true;

            if (!string.IsNullOrWhiteSpace(message.TextBody))
                mail.AlternateViews.Add(
                    AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));

            return smtp.SendMailAsync(mail, cancellationToken);
        }
#pragma warning restore SYSLIB0014
#pragma warning restore CA1416
    }
}
