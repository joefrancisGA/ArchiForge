using ArchLucid.Api.Attributes;
using ArchLucid.Api.Marketing;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Pilots;
using ArchLucid.Host.Core.Demo;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Marketing;

/// <summary>
/// Anonymous marketing artifact: bundled PDF proof pack for the public <c>/why</c> page, sourced only from
/// <see cref="IDemoCommitPagePreviewClient"/> (same deterministic demo data as <c>GET /v1/demo/preview</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/marketing")]
[EnableRateLimiting("fixed")]
[AllowAnonymous]
[FeatureGate(FeatureGateKey.DemoEnabled)]
public sealed class WhyArchlucidMarketingPackController(
    IDemoCommitPagePreviewClient previewClient,
    WhyArchLucidPackPdfBuilder pdfBuilder) : ControllerBase
{
    private readonly IDemoCommitPagePreviewClient _previewClient =
        previewClient ?? throw new ArgumentNullException(nameof(previewClient));

    private readonly WhyArchLucidPackPdfBuilder _pdfBuilder =
        pdfBuilder ?? throw new ArgumentNullException(nameof(pdfBuilder));

    /// <summary>
    /// Returns a single PDF (manifest excerpt, explanation, citations, timeline, plus sourced incumbent scaffold).
    /// </summary>
    [HttpGet("why-archlucid-pack.pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWhyArchlucidPackPdf(CancellationToken cancellationToken = default)
    {
        DemoCommitPagePreviewResponse? preview =
            await _previewClient.GetLatestCommittedDemoCommitPageAsync(cancellationToken);

        if (preview is null)
        {
            return this.NotFoundProblem(
                "No committed demo-seed run is available on this host. Run `archlucid try` or POST /v1/demo/seed and retry.",
                ProblemTypes.RunNotFound);
        }

        WhyArchLucidPackSourceDto source = WhyArchLucidPackSourceMapper.Map(preview);
        string markdown = WhyArchLucidPackBuilder.BuildMarkdown(source);
        byte[] pdf = _pdfBuilder.Build(markdown);

        return File(pdf, "application/pdf", "why-archlucid-pack.pdf");
    }
}
