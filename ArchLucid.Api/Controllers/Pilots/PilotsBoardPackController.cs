using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models.Pilots;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Pilots;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Pilots;

/// <summary>Quarterly board-pack PDF (Standard tier) — reuses digest + value-report builders.</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
[RequiresCommercialTenantTier(TenantTier.Standard)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/pilots")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public sealed class PilotsBoardPackController(BoardPackPdfBuilder boardPackPdfBuilder) : ControllerBase
{
    private readonly BoardPackPdfBuilder _boardPackPdfBuilder =
        boardPackPdfBuilder ?? throw new ArgumentNullException(nameof(boardPackPdfBuilder));

    /// <summary>Builds a quarterly sponsor board pack PDF for the current tenant scope.</summary>
    [HttpPost("board-pack.pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostBoardPackPdf(
        [FromBody] BoardPackPdfPostRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        try
        {
            string baseForLinks = $"{Request.Scheme}://{Request.Host.Value}";
            byte[] pdf = await _boardPackPdfBuilder.BuildPdfAsync(
                body.Year,
                body.Quarter,
                body.PeriodStartUtc,
                body.PeriodEndUtc,
                baseForLinks,
                cancellationToken);

            string name = $"board-pack-Q{body.Quarter}-{body.Year}.pdf";

            return File(pdf, "application/pdf", name);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
    }
}
