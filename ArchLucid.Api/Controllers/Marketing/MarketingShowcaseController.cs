using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Bootstrap;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Host.Core.Marketing;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Marketing;

/// <summary>Anonymous read-only hero payloads for Contoso runs flagged <c>IsPublicShowcase</c> in the demo catalog.</summary>
[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/marketing/showcase")]
[EnableRateLimiting("fixed")]
public sealed class MarketingShowcaseController(IPublicShowcaseCommitPageClient showcaseClient) : ControllerBase
{
    private readonly IPublicShowcaseCommitPageClient _showcaseClient =
        showcaseClient ?? throw new ArgumentNullException(nameof(showcaseClient));

    /// <summary>Resolves a run id (GUID or slug) to a commit-page-shaped JSON bundle.</summary>
    /// <param name="runKey">Canonical GUID, <c>N</c> hex, or slug <c>contoso-baseline</c> / <c>contoso-hardened</c>.</param>
    [HttpGet("{runKey}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DemoCommitPagePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShowcase(string runKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolveRunId(runKey, out Guid runId))
            return this.NotFoundProblem(
                "The showcase run key is not recognized.",
                type: ProblemTypes.ResourceNotFound);

        DemoCommitPagePreviewResponse? payload =
            await _showcaseClient.GetShowcaseCommitPageAsync(runId, cancellationToken);

        return payload is null ? this.NotFoundProblem("The showcase was not found.", type: ProblemTypes.ResourceNotFound) : Ok(payload);
    }

    private static bool TryResolveRunId(string runKey, out Guid runId)
    {
        runId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(runKey))
            return false;

        string trimmed = runKey.Trim();

        if (Guid.TryParse(trimmed, out runId))
            return true;

        string lowered = trimmed.ToLowerInvariant();

        if (lowered is "contoso-baseline" or "contoso-retail-baseline")
        {
            runId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId;

            return true;
        }

        if (lowered is not ("contoso-hardened" or "contoso-retail-hardened"))
            return trimmed.Length == 32 && IsHex32(trimmed) && Guid.TryParseExact(trimmed, "N", out runId);
        runId = ContosoRetailDemoIdentifiers.AuthorityRunHardenedId;

        return true;

        // 32-char hex without dashes (operator URLs often use "N" format).
    }

    private static bool IsHex32(string value)
    {
        return value.All(Uri.IsHexDigit);
    }
}
