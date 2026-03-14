using ArchiForge.Api.Models;
using ArchiForge.Api.Services;
using ArchiForge.Contracts.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
public sealed class ArchitectureController : ControllerBase
{
    private readonly IArchitectureApplicationService _applicationService;
    private readonly IHostEnvironment _environment;

    public ArchitectureController(
        IArchitectureApplicationService applicationService,
        IHostEnvironment environment)
    {
        _applicationService = applicationService;
        _environment = environment;
    }

    [HttpPost("request")]
    [ProducesResponseType(typeof(CreateArchitectureRunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRun(
        [FromBody] ArchitectureRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        var result = await _applicationService.CreateRunAsync(request, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(
            nameof(GetRun),
            new { runId = result.Response!.Run.RunId },
            result.Response);
    }

    [HttpGet("run/{runId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetRunAsync(runId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        return Ok(new
        {
            result.Run,
            result.Tasks,
            result.Results
        });
    }

    [HttpPost("run/{runId}/result")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAgentResult(
        [FromRoute] string runId,
        [FromBody] SubmitAgentResultRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Result is null)
        {
            return BadRequest(new { error = "Agent result is required." });
        }

        var result = await _applicationService.SubmitAgentResultAsync(runId, request.Result, cancellationToken);

        if (!result.Success)
        {
            if (result.Error?.Contains("was not found") == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

        return Accepted(new
        {
            message = "Agent result accepted.",
            runId,
            resultId = result.ResultId
        });
    }

    [HttpPost("run/{runId}/commit")]
    [ProducesResponseType(typeof(CommitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.CommitRunAsync(runId, cancellationToken);

        if (!result.Success)
        {
            var isNotFound = result.Errors.Any(e => e.Contains("not found"));
            if (isNotFound)
                return NotFound(new { error = result.Errors.First() });
            return BadRequest(new
            {
                errors = result.Errors,
                warnings = result.Warnings
            });
        }

        return Ok(result.Response);
    }

    [HttpGet("manifest/{version}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifest(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _applicationService.GetManifestAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { error = $"Manifest '{version}' was not found." });
        }

        return Ok(manifest);
    }

    [HttpPost("run/{runId}/seed-fake-results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeedFakeResults(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _applicationService.SeedFakeResultsAsync(runId, cancellationToken);

        if (!result.Success)
        {
            if (result.Error?.Contains("was not found") == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

        return Ok(new
        {
            message = "Fake results seeded.",
            runId,
            resultCount = result.ResultCount
        });
    }
}
