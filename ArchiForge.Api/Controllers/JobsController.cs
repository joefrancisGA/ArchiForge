using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Jobs;
using ArchiForge.Api.ProblemDetails;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Provides status and result-file access for background export jobs.
/// </summary>
/// <remarks>Routes under <c>v{version}/jobs</c>.</remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[Route("v{version:apiVersion}/jobs")]
[ApiVersion("1.0")]
public sealed class JobsController(IBackgroundJobQueue jobs) : ControllerBase
{
    /// <summary>Returns the current status of a background job.</summary>
    /// <param name="jobId">Background job identifier.</param>
    /// <returns>Job status, or 404 when the job is not found.</returns>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(BackgroundJobInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetJob([FromRoute] string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return this.BadRequestProblem("jobId is required.", ProblemTypes.ValidationFailed);

        var info = jobs.GetInfo(jobId);
        if (info is null)
            return this.NotFoundProblem($"Job '{jobId}' was not found.", ProblemTypes.ResourceNotFound);

        return Ok(info);
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
    [ProducesResponseType(typeof(BackgroundJobInfo), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BackgroundJobInfo), StatusCodes.Status409Conflict)]
    public IActionResult DownloadJobFile([FromRoute] string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return this.BadRequestProblem("jobId is required.", ProblemTypes.ValidationFailed);

        var info = jobs.GetInfo(jobId);
        if (info is null)
            return this.NotFoundProblem($"Job '{jobId}' was not found.", ProblemTypes.ResourceNotFound);

        if (info.State != BackgroundJobState.Succeeded)
            return Conflict(info);

        var file = jobs.GetFile(jobId);
        if (file is null)
            return Conflict(info);

        return File(file.Bytes, file.ContentType, file.FileName);
    }
}
