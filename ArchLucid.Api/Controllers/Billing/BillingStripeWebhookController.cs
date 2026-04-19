using System.Text;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Billing;
using ArchLucid.Persistence.Billing.Stripe;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Billing;

/// <summary>Stripe billing webhooks (signature verified inside <see cref="StripeBillingProvider"/>).</summary>
[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/billing/webhooks")]
public sealed class BillingStripeWebhookController(StripeBillingProvider stripeBillingProvider) : ControllerBase
{
    private readonly StripeBillingProvider _stripeBillingProvider =
        stripeBillingProvider ?? throw new ArgumentNullException(nameof(stripeBillingProvider));

    [HttpPost("stripe")]
    [Consumes("application/json")]
    public async Task<IActionResult> StripeAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        string rawBody;

        using (StreamReader reader = new(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))

            rawBody = await reader.ReadToEndAsync(cancellationToken);


        string signature = Request.Headers["Stripe-Signature"].ToString();

        BillingWebhookInbound inbound = new()
        {
            RawBody = rawBody,
            StripeSignatureHeader = string.IsNullOrWhiteSpace(signature) ? null : signature,
        };

        BillingWebhookHandleResult result =
            await _stripeBillingProvider.HandleWebhookAsync(inbound, cancellationToken);

        if (result.DuplicateIgnored || result.Succeeded)
            return Ok();


        return this.BadRequestProblem(result.ErrorDetail ?? "Stripe webhook rejected.", ProblemTypes.BadRequest);
    }
}
