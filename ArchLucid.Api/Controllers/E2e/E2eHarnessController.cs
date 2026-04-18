using System.Security.Cryptography;
using System.Text;

using ArchLucid.Api.Models.E2e;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Tenancy;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.Billing;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers.E2e;

/// <summary>
/// Non-production harness for live E2E (trial clock + billing activation). Gated by shared secret; returns 404 when disabled.
/// </summary>
[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/e2e")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class E2eHarnessController(
    IWebHostEnvironment environment,
    IOptionsMonitor<E2eHarnessOptions> harnessOptions,
    ITenantRepository tenantRepository,
    BillingWebhookTrialActivator billingWebhookTrialActivator) : ControllerBase
{
    private readonly IWebHostEnvironment _environment =
        environment ?? throw new ArgumentNullException(nameof(environment));

    private readonly IOptionsMonitor<E2eHarnessOptions> _harnessOptions =
        harnessOptions ?? throw new ArgumentNullException(nameof(harnessOptions));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly BillingWebhookTrialActivator _billingWebhookTrialActivator =
        billingWebhookTrialActivator ?? throw new ArgumentNullException(nameof(billingWebhookTrialActivator));

    [HttpPost("trial/set-expires")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetTrialExpiresAsync(
        [FromBody] E2eHarnessTrialExpiresPostRequest? body,
        CancellationToken cancellationToken)
    {
        if (!IsHarnessAuthorized())
        {
            return NotFound();
        }

        if (body is null || body.TenantId == Guid.Empty)
        {
            return NotFound();
        }

        await _tenantRepository.E2eHarnessSetTrialExpiresUtcAsync(body.TenantId, body.ExpiresUtc, cancellationToken);

        return NoContent();
    }

    [HttpPost("billing/simulate-subscription-activated")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SimulateSubscriptionActivatedAsync(
        [FromBody] E2eHarnessBillingSimulatePostRequest? body,
        CancellationToken cancellationToken)
    {
        if (!IsHarnessAuthorized())
        {
            return NotFound();
        }

        if (body is null ||
            body.TenantId == Guid.Empty ||
            body.WorkspaceId == Guid.Empty ||
            body.ProjectId == Guid.Empty ||
            string.IsNullOrWhiteSpace(body.ProviderSubscriptionId))
        {
            return NotFound();
        }

        if (!Enum.TryParse(body.CheckoutTier.Trim(), ignoreCase: true, out BillingCheckoutTier tier))
        {
            tier = BillingCheckoutTier.Team;
        }

        string tierStorageCode = BillingTierCode.FromCheckoutTier(tier);
        string checkoutLabel = BillingTierCode.CheckoutTierLabel(tier);
        string provider = string.IsNullOrWhiteSpace(body.Provider) ? "Noop" : body.Provider.Trim();
        string subscriptionId = body.ProviderSubscriptionId.Trim();
        string rawJson =
            $$"""{"simulated":true,"provider":"{{provider}}","subscription":"{{subscriptionId}}","tier":"{{checkoutLabel}}"}""";

        await _billingWebhookTrialActivator.OnSubscriptionActivatedAsync(
            body.TenantId,
            body.WorkspaceId,
            body.ProjectId,
            provider,
            subscriptionId,
            tierStorageCode,
            checkoutLabel,
            seats: 1,
            workspaces: 1,
            rawJson,
            cancellationToken);

        return NoContent();
    }

    private bool IsHarnessAuthorized()
    {
        if (_environment.IsProduction())
        {
            return false;
        }

        E2eHarnessOptions o = _harnessOptions.CurrentValue;

        if (!_environment.IsDevelopment() && !o.Enabled)
        {
            return false;
        }

        string? configured = o.SharedSecret?.Trim();

        if (string.IsNullOrEmpty(configured))
        {
            return false;
        }

        string? header = Request.Headers["X-ArchLucid-E2e-Harness-Secret"].FirstOrDefault();

        return ConstantTimeEquals(header, configured);
    }

    private static bool ConstantTimeEquals(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return false;
        }

        ReadOnlySpan<byte> ab = Encoding.UTF8.GetBytes(a);
        ReadOnlySpan<byte> bb = Encoding.UTF8.GetBytes(b);

        if (ab.Length != bb.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(ab, bb);
    }
}
