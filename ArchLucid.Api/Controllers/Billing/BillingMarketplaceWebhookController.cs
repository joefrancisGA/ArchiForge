using System.Text;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Billing;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Integration;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Billing.AzureMarketplace;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers.Billing;

/// <summary>Azure Marketplace SaaS fulfillment webhooks (JWT verified inside <see cref="AzureMarketplaceBillingProvider"/>).</summary>
[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/billing/webhooks")]
public sealed class BillingMarketplaceWebhookController(
    AzureMarketplaceBillingProvider marketplaceBillingProvider,
    IIntegrationEventOutboxRepository integrationEventOutbox,
    IIntegrationEventPublisher integrationEventPublisher,
    IOptionsMonitor<IntegrationEventsOptions> integrationEventsOptions,
    ILogger<BillingMarketplaceWebhookController> logger) : ControllerBase
{
    private readonly AzureMarketplaceBillingProvider _marketplaceBillingProvider =
        marketplaceBillingProvider ?? throw new ArgumentNullException(nameof(marketplaceBillingProvider));

    private readonly IIntegrationEventOutboxRepository _integrationEventOutbox =
        integrationEventOutbox ?? throw new ArgumentNullException(nameof(integrationEventOutbox));

    private readonly IIntegrationEventPublisher _integrationEventPublisher =
        integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));

    private readonly IOptionsMonitor<IntegrationEventsOptions> _integrationEventsOptions =
        integrationEventsOptions ?? throw new ArgumentNullException(nameof(integrationEventsOptions));

    private readonly ILogger<BillingMarketplaceWebhookController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpPost("marketplace")]
    [Consumes("application/json")]
    public async Task<IActionResult> MarketplaceAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        string rawBody;

        using (StreamReader reader = new(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        string? auth = Request.Headers.Authorization.ToString();

        string? bearer = null;

        if (!string.IsNullOrWhiteSpace(auth) &&
            auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            bearer = auth["Bearer ".Length..].Trim();
        }

        BillingWebhookInbound inbound = new()
        {
            RawBody = rawBody,
            MarketplaceAuthorizationBearer = bearer,
        };

        BillingWebhookHandleResult result =
            await _marketplaceBillingProvider.HandleWebhookAsync(inbound, cancellationToken);

        if (result.Succeeded && !result.DuplicateIgnored && result.MarketplaceWebhookReceived is not null)
        {
            await MarketplaceWebhookIntegrationEventPublisher.TryPublishAsync(
                _integrationEventOutbox,
                _integrationEventPublisher,
                _integrationEventsOptions.CurrentValue,
                _logger,
                result.MarketplaceWebhookReceived,
                cancellationToken);
        }

        if (result.DuplicateIgnored || result.Succeeded)
        {
            return Ok();
        }

        return this.BadRequestProblem(result.ErrorDetail ?? "Marketplace webhook rejected.", ProblemTypes.BadRequest);
    }
}
