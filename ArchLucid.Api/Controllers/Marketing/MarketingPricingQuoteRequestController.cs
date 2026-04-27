using System.Net;
using System.Security.Cryptography;
using System.Text;

using ArchLucid.Api.Models.Marketing;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Notifications.Email;
using ArchLucid.Contracts.Marketing;
using ArchLucid.Persistence.Marketing;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Marketing;

/// <summary>Anonymous quote-on-request for buyers who cannot use live checkout yet.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/marketing/pricing")]
[EnableRateLimiting("fixed")]
[AllowAnonymous]
public sealed class MarketingPricingQuoteRequestController(
    IMarketingPricingQuoteRequestRepository quoteRepository,
    IMarketingPricingQuoteSalesNotifier salesNotifier,
    ILogger<MarketingPricingQuoteRequestController> logger) : ControllerBase
{
    private const int MaxMessageChars = 2000;

    private readonly ILogger<MarketingPricingQuoteRequestController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMarketingPricingQuoteRequestRepository _quoteRepository =
        quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));

    private readonly IMarketingPricingQuoteSalesNotifier _salesNotifier =
        salesNotifier ?? throw new ArgumentNullException(nameof(salesNotifier));

    /// <summary>Append-only quote request (honeypot + rate limit).</summary>
    [HttpPost("quote-request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostQuoteRequest(
        [FromBody] MarketingPricingQuotePostRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        if (!string.IsNullOrWhiteSpace(body.WebsiteUrl))
        {
            // Silent success for bots — do not persist honeypot hits.
            return NoContent();
        }

        if (string.IsNullOrWhiteSpace(body.WorkEmail) || !body.WorkEmail.Contains('@', StringComparison.Ordinal))
            return this.BadRequestProblem("A valid work email is required.", ProblemTypes.ValidationFailed);

        if (string.IsNullOrWhiteSpace(body.CompanyName))
            return this.BadRequestProblem("Company name is required.", ProblemTypes.ValidationFailed);

        if (string.IsNullOrWhiteSpace(body.TierInterest))
            return this.BadRequestProblem("Tier interest is required.", ProblemTypes.ValidationFailed);

        if (body.Message.Length > MaxMessageChars)
            return this.BadRequestProblem($"Message must be at most {MaxMessageChars} characters.",
                ProblemTypes.ValidationFailed);

        byte[]? ipHash = TryHashRemoteIp(HttpContext);

        MarketingPricingQuoteRequestInsertResult? insert = await _quoteRepository.AppendAsync(
            body.WorkEmail.Trim(),
            body.CompanyName.Trim(),
            body.TierInterest.Trim(),
            body.Message.Trim(),
            ipHash,
            cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Marketing pricing quote request stored.");

        if (insert.HasValue)
        {
            await _salesNotifier.NotifyAsync(
                insert.Value,
                body.WorkEmail.Trim(),
                body.CompanyName.Trim(),
                body.TierInterest.Trim(),
                body.Message.Trim(),
                cancellationToken);
        }
        else if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Marketing pricing quote sales notification skipped (quote row not persisted — in-memory storage).");

        return NoContent();
    }

    private static byte[]? TryHashRemoteIp(HttpContext httpContext)
    {
        IPAddress? ip = httpContext.Connection.RemoteIpAddress;

        if (ip is null)
            return null;

        byte[] utf8 = Encoding.UTF8.GetBytes(ip.ToString());
        return SHA256.HashData(utf8);
    }
}
