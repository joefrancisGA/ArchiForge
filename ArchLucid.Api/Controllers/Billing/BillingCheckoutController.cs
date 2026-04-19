using System.Text.Json;

using ArchLucid.Api.Models.Billing;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Billing;

/// <summary>Hosted checkout for trial conversion (provider selected via <c>Billing:Provider</c>).</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenant/billing")]
public sealed class BillingCheckoutController(
    IBillingProviderRegistry billingProviderRegistry,
    IBillingLedger billingLedger,
    IScopeContextProvider scopeProvider,
    IAuditService auditService) : ControllerBase
{
    private readonly IBillingProviderRegistry _billingProviderRegistry =
        billingProviderRegistry ?? throw new ArgumentNullException(nameof(billingProviderRegistry));

    private readonly IBillingLedger _billingLedger = billingLedger ?? throw new ArgumentNullException(nameof(billingLedger));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    [HttpPost("checkout")]
    [SkipTrialWriteLimit]
    [Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
    [ProducesResponseType(typeof(BillingCheckoutResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckoutAsync(
        [FromBody] BillingCheckoutPostRequest? body,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        if (body is null ||
            string.IsNullOrWhiteSpace(body.ReturnUrl) ||
            string.IsNullOrWhiteSpace(body.CancelUrl))
        {
            IBillingProvider badReqProvider = _billingProviderRegistry.ResolveActiveProvider();
            ArchLucidInstrumentation.RecordBillingCheckout(badReqProvider.ProviderName, "unknown", "validation_failed");

            return this.BadRequestProblem(
                "ReturnUrl, CancelUrl, and TargetTier are required.",
                ProblemTypes.ValidationFailed);
        }

        BillingCheckoutTier tier = ParseCheckoutTier(body.TargetTier);

        if (await _billingLedger.TenantHasActiveSubscriptionAsync(scope.TenantId, cancellationToken))
        {
            IBillingProvider conflictProvider = _billingProviderRegistry.ResolveActiveProvider();
            ArchLucidInstrumentation.RecordBillingCheckout(
                conflictProvider.ProviderName,
                tier.ToString(),
                "conflict_active_subscription");

            return this.ConflictProblem(
                "An active billing subscription already exists for this tenant.",
                ProblemTypes.Conflict);
        }

        IBillingProvider providerForAudit = _billingProviderRegistry.ResolveActiveProvider();

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.BillingCheckoutInitiated,
                ActorUserId = User.Identity?.Name ?? "admin",
                ActorUserName = User.Identity?.Name ?? "admin",
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        provider = providerForAudit.ProviderName,
                        tier = tier.ToString(),
                    }),
            },
            cancellationToken);

        BillingCheckoutRequest request = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            TargetTier = tier,
            Seats = body.Seats,
            Workspaces = body.Workspaces,
            BillingEmail = body.BillingEmail,
            ReturnUrl = body.ReturnUrl.Trim(),
            CancelUrl = body.CancelUrl.Trim(),
        };

        IBillingProvider provider = _billingProviderRegistry.ResolveActiveProvider();

        BillingCheckoutResult result;

        try
        {
            result = await provider.CreateCheckoutSessionAsync(request, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ArchLucidInstrumentation.RecordBillingCheckout(provider.ProviderName, tier.ToString(), "provider_error");

            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }

        ArchLucidInstrumentation.RecordBillingCheckout(provider.ProviderName, tier.ToString(), "session_created");

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.BillingCheckoutCompleted,
                ActorUserId = User.Identity?.Name ?? "admin",
                ActorUserName = User.Identity?.Name ?? "admin",
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        provider = provider.ProviderName,
                        tier = tier.ToString(),
                        providerSessionId = result.ProviderSessionId,
                    }),
            },
            cancellationToken);

        return Ok(
            new BillingCheckoutResponseDto
            {
                CheckoutUrl = result.CheckoutUrl,
                ProviderSessionId = result.ProviderSessionId,
                ExpiresUtc = result.ExpiresUtc,
            });
    }

    private static BillingCheckoutTier ParseCheckoutTier(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return BillingCheckoutTier.Team;


        return label.Trim() switch
        {
            "Pro" => BillingCheckoutTier.Pro,
            "Enterprise" => BillingCheckoutTier.Enterprise,
            _ => BillingCheckoutTier.Team,
        };
    }
}
