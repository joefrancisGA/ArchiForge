using System.Globalization;
using System.Text.Json;

using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

using Stripe;
using Stripe.Checkout;

namespace ArchLucid.Persistence.Billing.Stripe;

public sealed class StripeBillingProvider(
    IOptionsMonitor<BillingOptions> billingOptions,
    IBillingLedger ledger,
    BillingWebhookTrialActivator trialActivator,
    IMarketplaceChangePlanWebhookMutationHandler changePlanWebhookMutationHandler) : IBillingProvider
{
    private readonly IOptionsMonitor<BillingOptions> _billingOptions =
        billingOptions ?? throw new ArgumentNullException(nameof(billingOptions));

    private readonly IMarketplaceChangePlanWebhookMutationHandler _changePlanWebhookMutationHandler =
        changePlanWebhookMutationHandler ?? throw new ArgumentNullException(nameof(changePlanWebhookMutationHandler));

    private readonly IBillingLedger _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

    private readonly BillingWebhookTrialActivator _trialActivator =
        trialActivator ?? throw new ArgumentNullException(nameof(trialActivator));

    public string ProviderName => BillingProviderNames.Stripe;

    public async Task<BillingCheckoutResult> CreateCheckoutSessionAsync(
        BillingCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        BillingOptions billing = _billingOptions.CurrentValue;
        string? secretKey = billing.Stripe.SecretKey?.Trim();

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Billing:Stripe:SecretKey is not configured.");


        string? priceId = ResolvePriceId(billing, request.TargetTier);

        if (string.IsNullOrWhiteSpace(priceId))

            throw new InvalidOperationException(
                "Stripe price id is not configured for the requested tier (Billing:Stripe:PriceIdTeam/Pro/Enterprise).");


        SessionService sessionService = new();

        SessionCreateOptions options = new()
        {
            Mode = "subscription",
            SuccessUrl = request.ReturnUrl,
            CancelUrl = request.CancelUrl,
            ClientReferenceId = request.TenantId.ToString("D", CultureInfo.InvariantCulture),
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = request.TenantId.ToString("D", CultureInfo.InvariantCulture),
                ["workspace_id"] = request.WorkspaceId.ToString("D", CultureInfo.InvariantCulture),
                ["project_id"] = request.ProjectId.ToString("D", CultureInfo.InvariantCulture),
                ["tier"] = BillingTierCode.CheckoutTierLabel(request.TargetTier),
                ["seats"] = Math.Max(1, request.Seats).ToString(CultureInfo.InvariantCulture),
                ["workspaces"] = Math.Max(1, request.Workspaces).ToString(CultureInfo.InvariantCulture)
            },
            LineItems =
            [
                new SessionLineItemOptions { Price = priceId, Quantity = 1 }
            ]
        };

        if (!string.IsNullOrWhiteSpace(request.BillingEmail))

            options.CustomerEmail = request.BillingEmail;


        RequestOptions requestOptions = new()
        {
            ApiKey = secretKey
        };

        Session session = await sessionService.CreateAsync(options, requestOptions, cancellationToken);

        string tierCode = BillingTierCode.FromCheckoutTier(request.TargetTier);

        await _ledger.UpsertPendingCheckoutAsync(
            request.TenantId,
            request.WorkspaceId,
            request.ProjectId,
            ProviderName,
            session.Id,
            tierCode,
            Math.Max(1, request.Seats),
            Math.Max(1, request.Workspaces),
            cancellationToken);

        // ReSharper disable once PatternAlwaysMatches
        DateTimeOffset? expiresUtc = session.ExpiresAt is DateTime dt
            ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
            : null;

        return new BillingCheckoutResult
        {
            CheckoutUrl = session.Url ?? string.Empty,
            ProviderSessionId = session.Id,
            ExpiresUtc = expiresUtc
        };
    }

    public async Task<BillingWebhookHandleResult> HandleWebhookAsync(
        BillingWebhookInbound inbound,
        CancellationToken cancellationToken)
    {
        BillingOptions billing = _billingOptions.CurrentValue;
        string? signingSecret = billing.Stripe.WebhookSigningSecret?.Trim();

        if (string.IsNullOrWhiteSpace(signingSecret) || string.IsNullOrWhiteSpace(inbound.StripeSignatureHeader))

            return BillingWebhookHandleResult.Rejected(
                "Stripe webhook signing secret or Stripe-Signature header is missing.");


        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                inbound.RawBody,
                inbound.StripeSignatureHeader,
                signingSecret,
                300,
                false);
        }
        catch (StripeException ex)
        {
            return BillingWebhookHandleResult.Rejected(ex.Message);
        }

        bool inserted = await _ledger.TryInsertWebhookEventAsync(
            stripeEvent.Id,
            ProviderName,
            stripeEvent.Type,
            inbound.RawBody,
            cancellationToken);

        if (!inserted)
        {
            string? prior = await _ledger.GetWebhookEventResultStatusAsync(stripeEvent.Id, cancellationToken);

            if (string.Equals(prior, "Processed", StringComparison.OrdinalIgnoreCase))
                return BillingWebhookHandleResult.Duplicate();
        }

        try
        {
            if (string.Equals(stripeEvent.Type, "checkout.session.completed", StringComparison.OrdinalIgnoreCase) &&
                stripeEvent.Data.Object is Session session)

                await HandleCheckoutSessionCompletedAsync(session, inbound.RawBody, cancellationToken);


            await _ledger.MarkWebhookProcessedAsync(stripeEvent.Id, "Processed", cancellationToken);

            return BillingWebhookHandleResult.Ok();
        }
        catch (Exception)
        {
            await _ledger.MarkWebhookProcessedAsync(stripeEvent.Id, "Failed", cancellationToken);

            throw;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(
        Session session,
        string rawBody,
        CancellationToken cancellationToken)
    {
        if (session.Metadata is null)
            return;


        if (!TryParseGuid(session.Metadata, "tenant_id", out Guid tenantId) ||
            !TryParseGuid(session.Metadata, "workspace_id", out Guid workspaceId) ||
            !TryParseGuid(session.Metadata, "project_id", out Guid projectId))

            return;


        BillingCheckoutTier checkoutTier = ParseCheckoutTier(session.Metadata, "tier");
        string tierCode = BillingTierCode.FromCheckoutTier(checkoutTier);
        int seats = ParsePositiveInt(session.Metadata, "seats", 1);
        int workspaces = ParsePositiveInt(session.Metadata, "workspaces", 1);
        string subscriptionId = session.SubscriptionId ?? session.Id;

        string planToken = checkoutTier switch
        {
            BillingCheckoutTier.Pro => "archlucid-stripe-pro",
            BillingCheckoutTier.Enterprise => "archlucid-stripe-enterprise",
            _ => "archlucid-stripe-team"
        };

        using JsonDocument planDoc = JsonDocument.Parse(
            JsonSerializer.Serialize(new Dictionary<string, string> { ["planId"] = planToken }));

        await _changePlanWebhookMutationHandler.HandleAsync(tenantId, planDoc.RootElement, rawBody, cancellationToken);

        await _trialActivator.OnSubscriptionActivatedAsync(
            tenantId,
            workspaceId,
            projectId,
            ProviderName,
            subscriptionId,
            tierCode,
            BillingTierCode.CheckoutTierLabel(checkoutTier),
            seats,
            workspaces,
            rawBody,
            cancellationToken);
    }

    private static bool TryParseGuid(Dictionary<string, string> metadata, string key, out Guid value)
    {
        value = Guid.Empty;

        if (!metadata.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
            return false;


        return Guid.TryParse(raw.Trim(), out value);
    }

    private static BillingCheckoutTier ParseCheckoutTier(Dictionary<string, string> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
            return BillingCheckoutTier.Team;


        return raw.Trim() switch
        {
            "Pro" => BillingCheckoutTier.Pro,
            "Enterprise" => BillingCheckoutTier.Enterprise,
            _ => BillingCheckoutTier.Team
        };
    }

    private static int ParsePositiveInt(Dictionary<string, string> metadata, string key, int fallback)
    {
        if (!metadata.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
            return fallback;


        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0
            ? n
            : fallback;
    }

    private static string? ResolvePriceId(BillingOptions billing, BillingCheckoutTier tier)
    {
        return tier switch
        {
            BillingCheckoutTier.Team => billing.Stripe.PriceIdTeam?.Trim(),
            BillingCheckoutTier.Pro => billing.Stripe.PriceIdPro?.Trim(),
            BillingCheckoutTier.Enterprise => billing.Stripe.PriceIdEnterprise?.Trim(),
            _ => billing.Stripe.PriceIdTeam?.Trim()
        };
    }
}
