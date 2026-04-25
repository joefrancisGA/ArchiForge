using ArchLucid.Application.Notifications.Email.Models;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Notifications;
using ArchLucid.Core.Notifications.Email;
using ArchLucid.Core.Tenancy;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Notifications.Email;

public sealed class TrialLifecycleEmailDispatcher(
    ITenantRepository tenantRepository,
    ITenantTrialEmailContactLookup contactLookup,
    IEmailTemplateRenderer templateRenderer,
    IEmailProvider emailProvider,
    ISentEmailLedger sentEmailLedger,
    IOptionsMonitor<EmailNotificationOptions> emailOptionsMonitor,
    ILogger<TrialLifecycleEmailDispatcher> logger) : ITrialLifecycleEmailDispatcher
{
    private const string DefaultProductName = "ArchLucid";

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly ITenantTrialEmailContactLookup _contactLookup =
        contactLookup ?? throw new ArgumentNullException(nameof(contactLookup));

    private readonly IEmailTemplateRenderer _templateRenderer =
        templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));

    private readonly IEmailProvider _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));

    private readonly ISentEmailLedger _sentEmailLedger =
        sentEmailLedger ?? throw new ArgumentNullException(nameof(sentEmailLedger));

    private readonly IOptionsMonitor<EmailNotificationOptions> _emailOptionsMonitor =
        emailOptionsMonitor ?? throw new ArgumentNullException(nameof(emailOptionsMonitor));

    private readonly ILogger<TrialLifecycleEmailDispatcher> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task DispatchAsync(TrialLifecycleEmailIntegrationEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.SchemaVersion != 1)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(
                    "Ignoring trial lifecycle email envelope with unsupported schemaVersion {SchemaVersion}.",
                    envelope.SchemaVersion);


            return;
        }

        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(envelope.TenantId, cancellationToken);

        if (tenant is null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning("Trial lifecycle email skipped; tenant {TenantId} not found.", envelope.TenantId);


            return;
        }

        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        if (!PassesTriggerGate(envelope.Trigger, tenant, utcNow))
            return;


        string? to = await _contactLookup.TryResolveAdminEmailAsync(envelope.TenantId, cancellationToken);

        if (string.IsNullOrWhiteSpace(to))
        {
            if (_logger.IsEnabled(LogLevel.Information))

                _logger.LogInformation(
                    "Trial lifecycle email skipped; no mailbox resolved for tenant {TenantId}.",
                    envelope.TenantId);


            return;
        }

        EmailNotificationOptions emailOptions = _emailOptionsMonitor.CurrentValue;
        string productName = string.IsNullOrWhiteSpace(emailOptions.ProductDisplayName)
            ? DefaultProductName
            : emailOptions.ProductDisplayName.Trim();

        string? baseUrl = string.IsNullOrWhiteSpace(emailOptions.OperatorBaseUrl)
            ? null
            : emailOptions.OperatorBaseUrl.TrimEnd('/');

        TrialDispatchPlan? plan = TryBuildPlan(envelope, tenant, productName, baseUrl, utcNow);

        if (plan is null)
            return;


        SentEmailLedgerEntry ledgerEntry = new(
            plan.IdempotencyKey,
            envelope.TenantId,
            plan.TemplateId,
            _emailProvider.ProviderName,
            ProviderMessageId: null);

        bool reserved = await _sentEmailLedger.TryRecordSentAsync(ledgerEntry, cancellationToken);

        if (!reserved)
            return;


        string html = await _templateRenderer
            .RenderHtmlAsync(plan.TemplateId, plan.Model, cancellationToken)
            .ConfigureAwait(false);

        string text = await _templateRenderer
            .RenderTextAsync(plan.TemplateId, plan.Model, cancellationToken)
            .ConfigureAwait(false);

        EmailMessage message = new()
        {
            To = to.Trim(),
            Subject = plan.Subject,
            HtmlBody = html,
            TextBody = text,
            IdempotencyKey = plan.IdempotencyKey,
            Tags = new EmailMessageTags
            {
                TenantId = envelope.TenantId,
                EventType = $"{IntegrationEventTypes.TrialLifecycleEmailV1}:{envelope.Trigger}",
            },
        };

        try
        {
            await _emailProvider.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Error))

                _logger.LogError(
                    ex,
                    "Trial lifecycle email send failed after idempotency reservation for tenant {TenantId}, template {TemplateId}.",
                    envelope.TenantId,
                    plan.TemplateId);


            throw;
        }
    }

    private static bool PassesTriggerGate(TrialLifecycleEmailTrigger trigger, TenantRecord tenant, DateTimeOffset utcNow)
    {
        if (trigger is TrialLifecycleEmailTrigger.Converted)
            return string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Converted, StringComparison.Ordinal);


        if (!string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal))
            return false;


        if (trigger is TrialLifecycleEmailTrigger.TrialProvisioned)
            return true;


        if (trigger is TrialLifecycleEmailTrigger.FirstRunCommitted)
            return tenant.TrialRunsLimit is not null && tenant.TrialRunsUsed == 1;


        if (trigger is TrialLifecycleEmailTrigger.MidTrialDay7)
        {
            if (tenant.TrialStartUtc is null)
                return false;


            if (tenant.TrialExpiresUtc is { } expMid && expMid <= utcNow)
                return false;


            return (utcNow - tenant.TrialStartUtc.Value).TotalDays >= 7d;
        }

        if (trigger is TrialLifecycleEmailTrigger.ApproachingRunLimit)
        {
            if (tenant.TrialRunsLimit is not { } limit || limit <= 0)
                return false;


            int threshold = (int)Math.Ceiling(limit * 0.8d);

            return tenant.TrialRunsUsed >= threshold;
        }

        if (trigger is TrialLifecycleEmailTrigger.ExpiringSoon)
        {
            if (tenant.TrialExpiresUtc is not { } expSoon)
                return false;

            if (expSoon <= utcNow)
                return false;

            return (expSoon - utcNow).TotalDays <= 2d;
        }

        if (trigger is not TrialLifecycleEmailTrigger.Expired)
            return false;
        if (tenant.TrialExpiresUtc is not { } expEnd)
            return false;

        return expEnd <= utcNow;
    }

    private TrialDispatchPlan? TryBuildPlan(
        TrialLifecycleEmailIntegrationEnvelope envelope,
        TenantRecord tenant,
        string productName,
        string? baseUrl,
        DateTimeOffset utcNow)
    {
        string idempotencyKey = TrialEmailIdempotencyKeys.ForTrigger(envelope.Trigger, envelope.TenantId);

        if (envelope.Trigger is TrialLifecycleEmailTrigger.TrialProvisioned)
        {
            TrialWelcomeEmailModel model = new(tenant.Name, productName);

            return new TrialDispatchPlan(
                idempotencyKey,
                EmailTemplateIds.TrialWelcome,
                Subject: $"Welcome to {productName}",
                model);
        }

        if (envelope.Trigger is TrialLifecycleEmailTrigger.FirstRunCommitted)
        {
            TrialFirstRunEmailModel model = new(productName, CombineUrl(baseUrl, "/welcome"));

            return new TrialDispatchPlan(
                idempotencyKey,
                EmailTemplateIds.TrialFirstRunComplete,
                Subject: "Your first architecture run completed",
                model);
        }

        if (envelope.Trigger is TrialLifecycleEmailTrigger.MidTrialDay7)
        {
            TrialMidTrialEmailModel model = new(productName, CombineUrl(baseUrl, "/welcome"));

            return new TrialDispatchPlan(
                idempotencyKey,
                EmailTemplateIds.TrialMidTrialDay7,
                Subject: $"Day 7 check-in — your {productName} trial",
                model);
        }

        if (envelope.Trigger is TrialLifecycleEmailTrigger.ApproachingRunLimit)
        {
            if (tenant.TrialRunsLimit is not { } limit)
                return null;


            TrialApproachingRunLimitEmailModel model = new(productName, tenant.TrialRunsUsed, limit);

            return new TrialDispatchPlan(
                idempotencyKey,
                EmailTemplateIds.TrialApproachingRunLimit,
                Subject: "Approaching your trial run limit",
                model);
        }

        if (envelope.Trigger is TrialLifecycleEmailTrigger.ExpiringSoon)
        {
            if (tenant.TrialExpiresUtc is null)
                return null;


            int daysRemaining = (int)Math.Max(0d, Math.Ceiling((tenant.TrialExpiresUtc.Value - utcNow).TotalDays));

            TrialExpiringSoonEmailModel model = new(productName, daysRemaining);

            return new TrialDispatchPlan(
                idempotencyKey,
                EmailTemplateIds.TrialExpiringSoon,
                Subject: "Your trial is ending soon",
                model);
        }

        if (envelope.Trigger is TrialLifecycleEmailTrigger.Expired)
        {
            TrialExpiredEmailModel model = new(productName, CombineUrl(baseUrl, "/welcome"));

            return new TrialDispatchPlan(
                idempotencyKey,
                EmailTemplateIds.TrialExpired,
                Subject: $"Your {productName} trial has ended",
                model);
        }

        if (envelope.Trigger is not TrialLifecycleEmailTrigger.Converted)
            return null;
        {
            string tier = string.IsNullOrWhiteSpace(envelope.TargetTier) ? tenant.Tier.ToString() : envelope.TargetTier.Trim();

            TrialConvertedEmailModel model = new(productName, tier);

            return new TrialDispatchPlan(
                idempotencyKey,
                EmailTemplateIds.TrialConverted,
                Subject: $"Welcome to {tier}",
                model);
        }

    }

    private static string CombineUrl(string? baseUrl, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return relativePath.StartsWith('/') ? relativePath : "/" + relativePath;


        string rel = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;

        return $"{baseUrl}{rel}";
    }

    private sealed record TrialDispatchPlan(string IdempotencyKey, string TemplateId, string Subject, object Model);
}
