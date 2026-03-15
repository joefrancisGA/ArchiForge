using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Contracts.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed class ArchitectureController : ControllerBase
{
    private readonly IArchitectureRunService _runService;
    private readonly IArchitectureApplicationService _applicationService;
    private readonly IHostEnvironment _environment;

    public ArchitectureController(
        IArchitectureRunService runService,
        IArchitectureApplicationService applicationService,
        IHostEnvironment environment)
    {
        _runService = runService;
        _applicationService = applicationService;
        _environment = environment;
    }

    [HttpPost("request")]
    [ProducesResponseType(typeof(CreateRunResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRun(
        [FromBody] ArchitectureRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Problem(
                type: ProblemTypes.RequestBodyRequired,
                title: "Request body is required.",
                detail: "The request body must contain a valid ArchitectureRequest.",
                statusCode: StatusCodes.Status400BadRequest,
                instance: HttpContext.Request.Path));
        }

        try
        {
            var result = await _runService.CreateRunAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetRun),
                new { runId = result.Run.RunId },
                result);
        }
        catch (InvalidOperationException ex)
        {
            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Type = ProblemTypes.ValidationFailed,
                Title = "Validation failed.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path,
                Extensions = { ["errors"] = new[] { ex.Message } }
            };
            return BadRequest(problem);
        }
    }

    [HttpPost("run/{runId}/execute")]
    [ProducesResponseType(typeof(ExecuteRunResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _runService.ExecuteRunAsync(runId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = ProblemTypes.RunNotFound,
                    Title = "Run or resource not found.",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path.ToString(),
                    Extensions = { ["runId"] = runId }
                });
            }
            return BadRequest(Problem(
                type: ProblemTypes.ValidationFailed,
                title: "Execute failed.",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                instance: HttpContext.Request.Path));
        }
    }

    [HttpGet("run/{runId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetRunAsync(runId, cancellationToken);
        if (result is null)
        {
            return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Type = ProblemTypes.RunNotFound,
                Title = "Run not found.",
                Detail = $"Run '{runId}' was not found.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path.ToString(),
                Extensions = { ["runId"] = runId }
            });
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
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAgentResult(
        [FromRoute] string runId,
        [FromBody] SubmitAgentResultRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Result is null)
        {
            return BadRequest(Problem(
                type: ProblemTypes.AgentResultRequired,
                title: "Agent result is required.",
                detail: "The request body must include a non-null result.",
                statusCode: StatusCodes.Status400BadRequest,
                instance: HttpContext.Request.Path));
        }

        var result = await _applicationService.SubmitAgentResultAsync(runId, request.Result, cancellationToken);

        if (!result.Success)
        {
            if (result.Error?.Contains("was not found") == true)
                return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = ProblemTypes.RunNotFound,
                    Title = "Run not found.",
                    Detail = result.Error,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path.ToString(),
                    Extensions = { ["runId"] = runId }
                });
            return BadRequest(Problem(
                type: ProblemTypes.ValidationFailed,
                title: "Submission failed.",
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                instance: HttpContext.Request.Path));
        }

        return Accepted(new
        {
            message = "Agent result accepted.",
            runId,
            resultId = result.ResultId
        });
    }

    [HttpPost("run/{runId}/commit")]
    [ProducesResponseType(typeof(CommitRunResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _runService.CommitRunAsync(runId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = ProblemTypes.RunNotFound,
                    Title = "Run not found.",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path.ToString(),
                    Extensions = { ["runId"] = runId }
                });
            }
            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Type = ProblemTypes.CommitFailed,
                Title = "Commit failed.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path,
                Extensions = { ["errors"] = new[] { ex.Message } }
            };
            return BadRequest(problem);
        }
    }

    [HttpGet("manifest/{version}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifest(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _applicationService.GetManifestAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Type = ProblemTypes.ManifestNotFound,
                Title = "Manifest not found.",
                Detail = $"Manifest '{version}' was not found.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path.ToString(),
                Extensions = { ["version"] = version }
            });
        }

        return Ok(manifest);
    }

    [HttpPost("run/{runId}/seed-fake-results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeedFakeResults(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound(Problem(
                type: ProblemTypes.UnavailableInProduction,
                title: "Not found.",
                detail: "This endpoint is only available in the Development environment.",
                statusCode: StatusCodes.Status404NotFound,
                instance: HttpContext.Request.Path));
        }

        var result = await _applicationService.SeedFakeResultsAsync(runId, cancellationToken);

        if (!result.Success)
        {
            if (result.Error?.Contains("was not found") == true)
                return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = ProblemTypes.RunNotFound,
                    Title = "Run not found.",
                    Detail = result.Error,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path.ToString(),
                    Extensions = { ["runId"] = runId }
                });
            return BadRequest(Problem(
                type: ProblemTypes.ValidationFailed,
                title: "Seed failed.",
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                instance: HttpContext.Request.Path));
        }

        return Ok(new
        {
            message = "Fake results seeded.",
            runId,
            resultCount = result.ResultCount
        });
    }
}
