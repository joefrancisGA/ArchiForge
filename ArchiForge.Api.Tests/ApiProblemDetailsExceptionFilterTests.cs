using ArchiForge.Api.ProblemDetails;
using ArchiForge.Application.Analysis;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ApiProblemDetailsExceptionFilterTests
{
    [Fact]
    public void ComparisonVerificationFailedException_Produces422WithDriftExtensions()
    {
        var filter = new ApiProblemDetailsExceptionFilter();
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Path = "/v1/architecture/comparisons/r1/replay"
            }
        };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor());
        var drift = new DriftAnalysisResult
        {
            DriftDetected = true,
            Summary = "payload mismatch"
        };
        var ex = new ComparisonVerificationFailedException("Verification failed.", drift);
#pragma warning disable IDE0028 // Simplify collection initialization
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = ex
        };
#pragma warning restore IDE0028 // Simplify collection initialization

        filter.OnException(context);

        context.ExceptionHandled.Should().BeTrue();
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(422);
        var problem = result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("driftDetected");
        problem.Extensions["driftDetected"].Should().Be(true);
        problem.Extensions["driftSummary"].Should().Be("payload mismatch");
    }
}
