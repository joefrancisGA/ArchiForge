using System.Net;
using ArchLucid.Application.Identity;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Notifications.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Notifications.Email;
public sealed class TrialLocalIdentityAccountExistsEmailNotifier(IEmailProvider emailProvider, IOptionsMonitor<EmailNotificationOptions> emailOptionsMonitor, ILogger<TrialLocalIdentityAccountExistsEmailNotifier> logger) : ITrialLocalIdentityAccountExistsNotifier
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(emailProvider, emailOptionsMonitor, logger);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Core.Notifications.Email.IEmailProvider emailProvider, Microsoft.Extensions.Options.IOptionsMonitor<ArchLucid.Core.Configuration.EmailNotificationOptions> emailOptionsMonitor, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.Notifications.Email.TrialLocalIdentityAccountExistsEmailNotifier> logger)
    {
        ArgumentNullException.ThrowIfNull(emailProvider);
        ArgumentNullException.ThrowIfNull(emailOptionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    private const string DefaultProductName = "ArchLucid";
    private const string TemplateId = "trial-local-identity-account-exists";
    private readonly IEmailProvider _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));
    private readonly IOptionsMonitor<EmailNotificationOptions> _emailOptionsMonitor = emailOptionsMonitor ?? throw new ArgumentNullException(nameof(emailOptionsMonitor));
    private readonly ILogger<TrialLocalIdentityAccountExistsEmailNotifier> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    /// <inheritdoc/>
    public async Task NotifyAccountAlreadyExistsAsync(string toEmail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toEmail);
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("Trial local account-exists email skipped: empty recipient.");
            return;
        }

        string trimmed = toEmail.Trim();
        EmailNotificationOptions emailOptions = _emailOptionsMonitor.CurrentValue;
        string productName = string.IsNullOrWhiteSpace(emailOptions.ProductDisplayName) ? DefaultProductName : emailOptions.ProductDisplayName.Trim();
        string safeProduct = WebUtility.HtmlEncode(productName);
        string subject = $"{productName}: sign-in request";
        string html = $"<p>You already have a {safeProduct} account for this email address.</p>" + "<p>If you forgot your password, use the password reset or sign-in flow for your environment.</p>" + "<p>If you did not try to register again, you can ignore this message.</p>";
        string text = $"You already have a {productName} account for this email address.\n" + "If you forgot your password, use the password reset or sign-in flow for your environment.\n" + "If you did not try to register again, you can ignore this message.\n";
        EmailMessage message = new()
        {
            To = trimmed,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
            IdempotencyKey = $"{TemplateId}:{TrialEmailNormalizer.Normalize(trimmed)}",
            Tags = new EmailMessageTags
            {
                TenantId = Guid.Empty,
                EventType = TemplateId
            }
        };
        try
        {
            await _emailProvider.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(ex, "Trial local account-exists email send failed for {Email}.", trimmed);
        }
    }
}