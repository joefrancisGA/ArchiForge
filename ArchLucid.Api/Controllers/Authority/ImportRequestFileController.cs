using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Import;
using ArchLucid.Core.Authorization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Authority;

/// <summary>Multipart import of architecture request drafts (TOML/JSON).</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ImportRequestFileController(
    IImportRequestFileService importRequestFileService,
    ILogger<ImportRequestFileController> logger) : ControllerBase
{
    private const long MaxMultipartBodyBytes = 512 * 1024 + 32 * 1024;

    /// <summary>
    ///     Upload a UTF-8 .toml or .json file (max 512 KB). Returns 202 with import id when the draft is stored; 422 on
    ///     validation, parse, or content-safety failure.
    /// </summary>
    /// <remarks>
    ///     Do not decorate <see cref="IFormFile" /> with <c>[FromForm]</c>: Swashbuckle throws
    ///     <c>SwaggerGeneratorException</c> while generating OpenAPI. ApiController still binds the file from
    ///     multipart form-data.
    /// </remarks>
    [HttpPost("request/import")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [RequestSizeLimit(MaxMultipartBodyBytes)]
    public async Task<IActionResult> ImportAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        ImportRequestFileResult result =
            await importRequestFileService.ImportAsync(file, cancellationToken, HttpContext.TraceIdentifier);

        if (result.Succeeded)
        {
            return Accepted(new { importId = result.ImportedRequestId, result.Status, result.Warnings });
        }

        string detail = BuildFailureDetail(result);

        logger.LogInformation("Architecture request file import rejected: {Detail}", detail);

        return this.UnprocessableEntityProblem(detail);
    }

    private static string BuildFailureDetail(ImportRequestFileResult result)
    {
        if (result.ValidationErrors is { Count: > 0 })
            return string.Join("; ", result.ValidationErrors);

        if (result.ContentSafetyReasons is { Count: > 0 })
            return string.Join("; ", result.ContentSafetyReasons);

        return result.FailureDetail ?? "Import failed.";
    }
}
