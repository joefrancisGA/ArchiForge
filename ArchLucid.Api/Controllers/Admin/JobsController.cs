using ArchLucid.Core.Authorization;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Jobs;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>
/// Provides status and result-file access for background export jobs.
/// </summary>
/// <remarks>Routes under <c>v{version}/jobs</c>.</remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[Route("v{version:apiVersion}/jobs")]
[ApiVersion("1.0")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[EnableRateLimiting("fixed")]
public sealed class JobsController(IBackgroundJobQueue jobs) : ControllerBase
{
    /// <summary>Returns the current status of a background job.</summary>
    /// <param name="jobId">Background job identifier.</param>
    /// <returns>Job status, or 404 when the job is not found.</returns>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(BackgroundJobInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJob([FromRoute] string jobId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return this.BadRequestProblem("jobId is required.", ProblemTypes.ValidationFailed);

        BackgroundJobInfo? info = await jobs.GetInfoAsync(jobId, cancellationToken);
        return info is null ? this.NotFoundProblem($"Job '{jobId}' was not found.", ProblemTypes.ResourceNotFound) : Ok(info);
    }

    /// <summary>Downloads the result file produced by a completed background job.</summary>
    /// <param name="jobId">Background job identifier.</param>
    /// <returns>
    /// The file bytes with the appropriate content type on success;
    /// 404 when the job is not found;
    /// 409 when the job has not yet succeeded or has no file attached.
    /// </returns>
    [HttpGet("{jobId}/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DownloadJobFile([FromRoute] string jobId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return this.BadRequestProblem("jobId is required.", ProblemTypes.ValidationFailed);

        BackgroundJobInfo? info = await jobs.GetInfoAsync(jobId, cancellationToken);
        if (info is null)
            return this.NotFoundProblem($"Job '{jobId}' was not found.", ProblemTypes.ResourceNotFound);

        if (info.State != BackgroundJobState.Succeeded)
            return this.ConflictProblem(
                $"Job '{jobId}' has not completed successfully (state: {info.State}).",
                ProblemTypes.Conflict);


        BackgroundJobFile? file = await jobs.GetFileAsync(jobId, cancellationToken);

        if (file is null)
            return this.ConflictProblem(
                $"Job '{jobId}' succeeded but no result file is available.",
                ProblemTypes.Conflict);


        return File(file.Bytes, file.ContentType, file.FileName);
    }
}
