using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[Route("v{version:apiVersion}/jobs")]
[ApiVersion("1.0")]
public sealed class JobsController(IBackgroundJobQueue jobs) : ControllerBase
{
    [HttpGet("{jobId}")]
    public IActionResult GetJob([FromRoute] string jobId)
    {
        var info = jobs.GetInfo(jobId);
        if (info is null) return NotFound();
        return Ok(info);
    }

    [HttpGet("{jobId}/file")]
    public IActionResult DownloadJobFile([FromRoute] string jobId)
    {
        var info = jobs.GetInfo(jobId);
        if (info is null) return NotFound();
        if (info.State != BackgroundJobState.Succeeded) return Conflict(info);

        var file = jobs.GetFile(jobId);
        if (file is null) return Conflict(info);

        return File(file.Bytes, file.ContentType, file.FileName);
    }
}

