using System.Text;

using ArchLucid.Api.Marketing;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Pilots;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Marketing;

/// <summary>Anonymous marketing artifact: printable PDF from the canonical Executive Sponsor Brief markdown.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/marketing")]
[EnableRateLimiting("fixed")]
[AllowAnonymous]
public sealed class SponsorBriefMarketingController(
    IWebHostEnvironment hostEnvironment,
    ExecutiveSponsorBriefPdfBuilder pdfBuilder) : ControllerBase
{
    private readonly ExecutiveSponsorBriefPdfBuilder _pdfBuilder =
        pdfBuilder ?? throw new ArgumentNullException(nameof(pdfBuilder));

    private readonly IWebHostEnvironment _hostEnvironment =
        hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));

    /// <summary>Returns the sponsor brief as PDF — content matches <c>docs/EXECUTIVE_SPONSOR_BRIEF.md</c> on disk.</summary>
    [HttpGet("sponsor-brief.pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetSponsorBriefPdf()
    {
        string? path = RepositoryDocsMarkdownPath.TryFindFile(_hostEnvironment, "EXECUTIVE_SPONSOR_BRIEF.md");

        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            return this.NotFoundProblem(
                $"Executive sponsor brief was not found under docs/ (started from '{_hostEnvironment.ContentRootPath}').",
                ProblemTypes.ResourceNotFound);
        }

        string markdown = System.IO.File.ReadAllText(path, Encoding.UTF8);
        byte[] pdf = _pdfBuilder.Build(markdown);

        return File(pdf, "application/pdf", "sponsor-brief.pdf");
    }
}
