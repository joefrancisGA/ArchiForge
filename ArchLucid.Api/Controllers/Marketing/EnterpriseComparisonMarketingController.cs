using System.Text;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Pilots;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Marketing;

/// <summary>Anonymous marketing PDF built from <c>docs/go-to-market/ENTERPRISE_COMPARISON_ONE_PAGE.md</c>.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/marketing")]
[EnableRateLimiting("fixed")]
[AllowAnonymous]
public sealed class EnterpriseComparisonMarketingController(
    IWebHostEnvironment hostEnvironment,
    WhyArchLucidPackPdfBuilder pdfBuilder) : ControllerBase
{
    private readonly IWebHostEnvironment _hostEnvironment =
        hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));

    private readonly WhyArchLucidPackPdfBuilder _pdfBuilder =
        pdfBuilder ?? throw new ArgumentNullException(nameof(pdfBuilder));

    /// <summary>Returns a single-page PDF sourced from the repository Markdown file (read-only file IO).</summary>
    [HttpGet("enterprise-comparison.pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetEnterpriseComparisonPdf()
    {
        string path = Path.GetFullPath(
            Path.Combine(_hostEnvironment.ContentRootPath, "..", "..", "docs", "go-to-market", "ENTERPRISE_COMPARISON_ONE_PAGE.md"));

        if (!System.IO.File.Exists(path))
        {
            return this.NotFoundProblem(
                $"Enterprise comparison Markdown was not found at '{path}'.",
                ProblemTypes.ResourceNotFound);
        }

        string markdown = System.IO.File.ReadAllText(path, Encoding.UTF8);
        byte[] pdf = _pdfBuilder.Build(markdown);

        return File(pdf, "application/pdf", "enterprise-comparison.pdf");
    }
}
