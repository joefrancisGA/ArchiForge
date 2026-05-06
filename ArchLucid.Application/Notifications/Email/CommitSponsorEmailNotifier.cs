using System.Net;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Notifications.Email;
using ArchLucid.Core.Tenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Notifications.Email;
public sealed class CommitSponsorEmailNotifier(ITenantTrialEmailContactLookup contactLookup, IEmailProvider emailProvider, IOptionsMonitor<EmailNotificationOptions> emailOptionsMonitor, ILogger<CommitSponsorEmailNotifier> logger) : ICommitSponsorEmailNotifier
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(contactLookup, emailProvider, emailOptionsMonitor, logger);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Core.Tenancy.ITenantTrialEmailContactLookup contactLookup, ArchLucid.Core.Notifications.Email.IEmailProvider emailProvider, Microsoft.Extensions.Options.IOptionsMonitor<ArchLucid.Core.Configuration.EmailNotificationOptions> emailOptionsMonitor, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.Notifications.Email.CommitSponsorEmailNotifier> logger)
    {
        ArgumentNullException.ThrowIfNull(contactLookup);
        ArgumentNullException.ThrowIfNull(emailProvider);
        ArgumentNullException.ThrowIfNull(emailOptionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    private const string DefaultProductName = "ArchLucid";
    private const string TemplateId = "architecture-commit-sponsor";
    private readonly ITenantTrialEmailContactLookup _contactLookup = contactLookup ?? throw new ArgumentNullException(nameof(contactLookup));
    private readonly IOptionsMonitor<EmailNotificationOptions> _emailOptionsMonitor = emailOptionsMonitor ?? throw new ArgumentNullException(nameof(emailOptionsMonitor));
    private readonly IEmailProvider _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));
    private readonly ILogger<CommitSponsorEmailNotifier> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    /// <inheritdoc/>
    public async Task NotifyAfterCommitAsync(Guid tenantId, string runId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runId);
        if (tenantId == Guid.Empty)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("Commit sponsor email skipped: empty tenant id for run {RunId}.", runId);
            return;
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("Commit sponsor email skipped: empty run id for tenant {TenantId}.", tenantId);
            return;
        }

        string trimmedRunId = runId.Trim();
        string? to = await _contactLookup.TryResolveAdminEmailAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(to))
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Commit sponsor email skipped: no admin mailbox for tenant {TenantId}, run {RunId}.", tenantId, trimmedRunId);
            return;
        }

        EmailNotificationOptions emailOptions = _emailOptionsMonitor.CurrentValue;
        string productName = string.IsNullOrWhiteSpace(emailOptions.ProductDisplayName) ? DefaultProductName : emailOptions.ProductDisplayName.Trim();
        string? operatorBase = string.IsNullOrWhiteSpace(emailOptions.OperatorBaseUrl) ? null : emailOptions.OperatorBaseUrl.Trim().TrimEnd('/');
        string runUrlText = operatorBase is null ? $"(configure {nameof(EmailNotificationOptions.OperatorBaseUrl)}) /runs/{trimmedRunId}" : $"{operatorBase}/runs/{Uri.EscapeDataString(trimmedRunId)}";
        string runUrlHref = operatorBase is null ? "#" : $"{operatorBase}/runs/{Uri.EscapeDataString(trimmedRunId)}";
        string safeRun = WebUtility.HtmlEncode(trimmedRunId);
        string safeProduct = WebUtility.HtmlEncode(productName);
        string safeUrlText = WebUtility.HtmlEncode(runUrlText);
        string subject = $"{productName}: architecture run finalized — sponsor link";
        string html = $"<p>{safeProduct}: an architecture run was finalized.</p>" + $"<p>Run id: <strong>{safeRun}</strong></p>" + $"<p>Operator link: <a href=\"{WebUtility.HtmlEncode(runUrlHref)}\">{safeUrlText}</a></p>" + "<p>This message was sent because someone chose to notify the tenant admin contact when finalizing the manifest.</p>";
        string text = $"{productName}: an architecture run was finalized.\n" + $"Run id: {trimmedRunId}\n" + $"Link: {runUrlText}\n";
        string idempotencyKey = $"architecture-commit-sponsor:{tenantId:N}:{trimmedRunId}";
        EmailMessage message = new()
        {
            To = to.Trim(),
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
            IdempotencyKey = idempotencyKey,
            Tags = new EmailMessageTags
            {
                TenantId = tenantId,
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
                _logger.LogWarning(ex, "Commit sponsor email send failed for tenant {TenantId}, run {RunId}.", tenantId, trimmedRunId);
        }
    }
}