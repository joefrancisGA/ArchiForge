using ArchiForge.Api.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
[Route("v{version:apiVersion}/jobs")]
[ApiVersion("1.0")]
public sealed class JobsController : ControllerBase
{
    private readonly IBackgroundJobQueue _jobs;

    public JobsController(IBackgroundJobQueue jobs)
    {
        _jobs = jobs;
    }

    [HttpGet("{jobId}")]
    public IActionResult GetJob([FromRoute] string jobId)
    {
        var info = _jobs.GetInfo(jobId);
        if (info is null) return NotFound();
        return Ok(info);
    }

    [HttpGet("{jobId}/file")]
    public IActionResult DownloadJobFile([FromRoute] string jobId)
    {
        var info = _jobs.GetInfo(jobId);
        if (info is null) return NotFound();
        if (info.State != BackgroundJobState.Succeeded) return Conflict(info);

        var file = _jobs.GetFile(jobId);
        if (file is null) return Conflict(info);

        return File(file.Bytes, file.ContentType, file.FileName);
    }
}

