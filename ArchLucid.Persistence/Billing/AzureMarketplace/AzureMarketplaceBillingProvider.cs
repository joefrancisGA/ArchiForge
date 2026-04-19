using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;

using Azure.Core;
using Azure.Identity;

using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Billing.AzureMarketplace;

public sealed class AzureMarketplaceBillingProvider(
    IOptionsMonitor<BillingOptions> billingOptions,
    IBillingLedger ledger,
    BillingWebhookTrialActivator trialActivator,
    IMarketplaceWebhookTokenVerifier tokenVerifier,
    IHttpClientFactory httpClientFactory) : IBillingProvider
{
    private readonly IOptionsMonitor<BillingOptions> _billingOptions =
        billingOptions ?? throw new ArgumentNullException(nameof(billingOptions));

    private readonly IBillingLedger _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

    private readonly BillingWebhookTrialActivator _trialActivator =
        trialActivator ?? throw new ArgumentNullException(nameof(trialActivator));

    private readonly IMarketplaceWebhookTokenVerifier _tokenVerifier =
        tokenVerifier ?? throw new ArgumentNullException(nameof(tokenVerifier));

    private readonly IHttpClientFactory _httpClientFactory =
        httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public string ProviderName => BillingProviderNames.AzureMarketplace;

    public async Task<BillingCheckoutResult> CreateCheckoutSessionAsync(
        BillingCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        BillingOptions billing = _billingOptions.CurrentValue;
        string? landing = billing.AzureMarketplace.LandingPageUrl?.Trim();

        if (string.IsNullOrWhiteSpace(landing))
        {
            throw new InvalidOperationException("Billing:AzureMarketplace:LandingPageUrl is not configured.");
        }

        string sessionId = $"mkt_sess_{Guid.NewGuid():N}";
        string join = landing.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        string url =
            $"{landing}{join}tenantId={Uri.EscapeDataString(request.TenantId.ToString("D", CultureInfo.InvariantCulture))}"
            + $"&workspaceId={Uri.EscapeDataString(request.WorkspaceId.ToString("D", CultureInfo.InvariantCulture))}"
            + $"&projectId={Uri.EscapeDataString(request.ProjectId.ToString("D", CultureInfo.InvariantCulture))}"
            + $"&tier={Uri.EscapeDataString(BillingTierCode.CheckoutTierLabel(request.TargetTier))}"
            + $"&session={Uri.EscapeDataString(sessionId)}";

        string tierCode = BillingTierCode.FromCheckoutTier(request.TargetTier);

        await _ledger.UpsertPendingCheckoutAsync(
            request.TenantId,
            request.WorkspaceId,
            request.ProjectId,
            ProviderName,
            sessionId,
            tierCode,
            Math.Max(1, request.Seats),
            Math.Max(1, request.Workspaces),
            cancellationToken);

        return new BillingCheckoutResult
        {
            CheckoutUrl = url,
            ProviderSessionId = sessionId,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
        };
    }

    public async Task<BillingWebhookHandleResult> HandleWebhookAsync(
        BillingWebhookInbound inbound,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(inbound.MarketplaceAuthorizationBearer))
        {
            return BillingWebhookHandleResult.Rejected("Missing Marketplace bearer token.");
        }

        System.Security.Claims.ClaimsPrincipal? principal =
            await _tokenVerifier.ValidateAsync(inbound.MarketplaceAuthorizationBearer, cancellationToken);

        if (principal is null)
        {
            return BillingWebhookHandleResult.Rejected("Marketplace JWT validation failed.");
        }

        using JsonDocument doc = JsonDocument.Parse(inbound.RawBody);
        JsonElement root = doc.RootElement;

        string action = root.TryGetProperty("action", out JsonElement actionEl)
            ? actionEl.GetString() ?? string.Empty
            : string.Empty;

        string subscriptionId = root.TryGetProperty("subscriptionId", out JsonElement subEl)
            ? subEl.GetString() ?? string.Empty
            : string.Empty;

        string dedupeKey = $"{subscriptionId}|{action}|{root.GetRawText().GetHashCode(StringComparison.Ordinal):X8}";

        bool inserted = await _ledger.TryInsertWebhookEventAsync(
            dedupeKey,
            ProviderName,
            action,
            inbound.RawBody,
            cancellationToken);

        if (!inserted)
        {
            string? prior = await _ledger.GetWebhookEventResultStatusAsync(dedupeKey, cancellationToken);

            if (string.Equals(prior, "Processed", StringComparison.OrdinalIgnoreCase))
            {
                return BillingWebhookHandleResult.Duplicate();
            }
        }

        try
        {
            Guid tenantId = ResolveTenantId(root, principal);

            if (tenantId == Guid.Empty)
            {
                await _ledger.MarkWebhookProcessedAsync(dedupeKey, "IgnoredMissingTenant", cancellationToken);

                return BillingWebhookHandleResult.Ok();
            }

            Guid workspaceId = ReadGuid(root, "workspaceId", Core.Scoping.ScopeIds.DefaultWorkspace);
            Guid projectId = ReadGuid(root, "projectId", Core.Scoping.ScopeIds.DefaultProject);

            await DispatchMarketplaceActionAsync(
                action,
                tenantId,
                workspaceId,
                projectId,
                subscriptionId,
                inbound.RawBody,
                cancellationToken);

            await _ledger.MarkWebhookProcessedAsync(dedupeKey, "Processed", cancellationToken);

            MarketplaceWebhookReceivedIntegrationPayload integrationPayload = new()
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                ProviderDedupeKey = dedupeKey,
                Action = action,
                SubscriptionId = subscriptionId,
                BillingProvider = ProviderName,
            };

            return BillingWebhookHandleResult.Ok(integrationPayload);
        }
        catch (Exception)
        {
            await _ledger.MarkWebhookProcessedAsync(dedupeKey, "Failed", cancellationToken);

            throw;
        }
    }

    private async Task DispatchMarketplaceActionAsync(
        string action,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string subscriptionId,
        string rawBody,
        CancellationToken cancellationToken)
    {
        string normalized = action.Trim();

        if (string.Equals(normalized, "Suspend", StringComparison.OrdinalIgnoreCase))
        {
            await _ledger.SuspendSubscriptionAsync(tenantId, cancellationToken);

            return;
        }

        if (string.Equals(normalized, "Reinstate", StringComparison.OrdinalIgnoreCase))
        {
            await _ledger.ReinstateSubscriptionAsync(tenantId, cancellationToken);

            return;
        }

        if (string.Equals(normalized, "Unsubscribe", StringComparison.OrdinalIgnoreCase))
        {
            await _ledger.CancelSubscriptionAsync(tenantId, cancellationToken);

            return;
        }

        if (string.Equals(normalized, "ChangePlan", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "ChangeQuantity", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(normalized, "Subscribe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "Purchase", StringComparison.OrdinalIgnoreCase))
        {
            await ActivateIfRequestedAsync(
                tenantId,
                workspaceId,
                projectId,
                subscriptionId,
                rawBody,
                cancellationToken);
        }
    }

    private async Task ActivateIfRequestedAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string subscriptionId,
        string rawBody,
        CancellationToken cancellationToken)
    {
        BillingOptions billing = _billingOptions.CurrentValue;

        if (billing.AzureMarketplace.FulfillmentApiEnabled)
        {
            using HttpClient client = _httpClientFactory.CreateClient(nameof(AzureMarketplaceBillingProvider));

            TokenCredential credential = new DefaultAzureCredential();
            AccessToken token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://marketplaceapi.microsoft.com/.default" }),
                cancellationToken);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            string activateUrl =
                $"https://marketplaceapi.microsoft.com/api/saas/subscriptions/{Uri.EscapeDataString(subscriptionId)}/activate?api-version=2018-08-31";

            using HttpResponseMessage response = await client.PostAsync(activateUrl, null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);

                throw new InvalidOperationException(
                    $"Marketplace activate failed with {(int)response.StatusCode}: {body}");
            }
        }

        BillingCheckoutTier tier = BillingCheckoutTier.Team;
        string tierCode = BillingTierCode.FromCheckoutTier(tier);

        await _trialActivator.OnSubscriptionActivatedAsync(
            tenantId,
            workspaceId,
            projectId,
            ProviderName,
            subscriptionId,
            tierCode,
            BillingTierCode.CheckoutTierLabel(tier),
            1,
            1,
            rawBody,
            cancellationToken);
    }

    private static Guid ReadGuid(JsonElement root, string name, Guid fallback)
    {
        if (!root.TryGetProperty(name, out JsonElement el))
        {
            return fallback;
        }

        string? s = el.GetString();

        return Guid.TryParse(s, out Guid g) ? g : fallback;
    }

    private Guid ResolveTenantId(JsonElement root, System.Security.Claims.ClaimsPrincipal principal)
    {
        BillingOptions billing = _billingOptions.CurrentValue;
        string? claimType = billing.AzureMarketplace.TenantIdClaimType?.Trim();

        if (!string.IsNullOrWhiteSpace(claimType))
        {
            string? fromClaim = principal.FindFirst(claimType)?.Value;

            if (Guid.TryParse(fromClaim, out Guid tenantFromClaim))
            {
                return tenantFromClaim;
            }
        }

        if (root.TryGetProperty("purchaser", out JsonElement purchaser) &&
            purchaser.TryGetProperty("tenantId", out JsonElement tenantEl))
        {
            string? s = tenantEl.GetString();

            if (Guid.TryParse(s, out Guid g))
            {
                return g;
            }
        }

        return Guid.Empty;
    }
}
