using System.Data.Common;

using ArchiForge.Api.ProblemDetails;
using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Core.Resilience;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Tests for Api Problem Details Exception Filter.
/// </summary>

[Trait("Category", "Unit")]
public sealed class ApiProblemDetailsExceptionFilterTests
{
    [Fact]
    public void ComparisonVerificationFailedException_Produces422WithDriftExtensions()
    {
        DriftAnalysisResult drift = new()
        {
            DriftDetected = true,
            Summary = "payload mismatch"
        };
        ComparisonVerificationFailedException ex = new("Verification failed.", drift);

        ExceptionContext context = CreateExceptionContext(ex, "/v1/architecture/comparisons/r1/replay");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(422);
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("driftDetected");
        problem.Extensions["driftDetected"].Should().Be(true);
        problem.Extensions["driftSummary"].Should().Be("payload mismatch");
        problem.Extensions["errorCode"].Should().Be(ProblemErrorCodes.ComparisonVerificationFailed);
        problem.Extensions.Should().ContainKey("supportHint");
        ((string)problem.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("drift");
    }

    [Fact]
    public void ConflictException_Produces409()
    {
        ExceptionContext context = CreateExceptionContext(new ConflictException("state"), "/v1/run/r/commit");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.Conflict);
        p.Extensions["errorCode"].Should().Be(ProblemErrorCodes.Conflict);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("idempotency");
    }

    [Fact]
    public void RunNotFoundException_Produces404()
    {
        ExceptionContext context = CreateExceptionContext(new RunNotFoundException("missing"), "/v1/run/missing");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.RunNotFound);
        p.Extensions["errorCode"].Should().Be(ProblemErrorCodes.RunNotFound);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("scope");
    }

    [Fact]
    public void InvalidOperationException_Produces400BadRequestType()
    {
        ExceptionContext context = CreateExceptionContext(new InvalidOperationException("bad"), "/v1/x");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.BadRequest);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("swagger");
    }

    [Fact]
    public void ArgumentException_Produces400ValidationType()
    {
        ExceptionContext context = CreateExceptionContext(new ArgumentException("arg"), "/v1/x");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.ValidationFailed);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("validation");
    }

    [Fact]
    public void UnmappedException_LeavesContextUnchanged()
    {
        ExceptionContext context = CreateExceptionContext(new NotSupportedException(), "/v1/x");

        RunFilter(context);

        context.ExceptionHandled.Should().BeFalse();
        context.Result.Should().BeNull();
    }

    [Fact]
    public void TimeoutException_Produces503DatabaseTimeout()
    {
        ExceptionContext context = CreateExceptionContext(
            new TimeoutException("Operation timed out"), "/v1/alerts");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.DatabaseTimeout);
        p.Title.Should().Be("Request Timeout");
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("health/ready");
    }

    [Fact]
    public void TryMapDatabaseException_SqlTimeoutException_Returns503()
    {
        // SqlException(-2) cannot be constructed directly; test via the mapper with a TimeoutException
        // which covers the same code path. The SqlException branch is tested implicitly by
        // the health check tests that use real SqlConnection failures.
        bool mapped = ApplicationProblemMapper.TryMapDatabaseException(
            new TimeoutException("sql timeout"), "/v1/runs", out ObjectResult? result);

        mapped.Should().BeTrue();
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public void TryMapDatabaseException_GenericDbException_Returns503DatabaseUnavailable()
    {
        bool mapped = ApplicationProblemMapper.TryMapDatabaseException(
            new TestDbException("connection refused"), "/v1/runs", out ObjectResult? result);

        mapped.Should().BeTrue();
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.DatabaseUnavailable);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("health/ready");
    }

    [Fact]
    public void CircuitBreakerOpenException_Produces503WithProblemTypeAndRetryExtension()
    {
        DateTimeOffset retryAfter = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        ExceptionContext context = CreateExceptionContext(
            new CircuitBreakerOpenException(retryAfter),
            "/v1/ask");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        Microsoft.AspNetCore.Mvc.ProblemDetails p =
            result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.CircuitBreakerOpen);
        p.Extensions["errorCode"].Should().Be(ProblemErrorCodes.CircuitBreakerOpen);
        p.Extensions.Should().ContainKey("retryAfterUtc");
        p.Extensions["retryAfterUtc"].Should().Be(retryAfter);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("azure openai");
    }

    [Fact]
    public void TryMapDatabaseException_NonDatabaseException_ReturnsFalse()
    {
        bool mapped = ApplicationProblemMapper.TryMapDatabaseException(
            new InvalidOperationException("not a db error"), "/v1/runs", out ObjectResult? result);

        mapped.Should().BeFalse();
        result.Should().BeNull();
    }

    /// <summary>Concrete <see cref="DbException"/> for test purposes (abstract class cannot be instantiated).</summary>
    private sealed class TestDbException(string message) : DbException(message);

    private static ExceptionContext CreateExceptionContext(Exception ex, string path)
    {
        DefaultHttpContext httpContext = new()
        {
            Request = { Path = path }
        };
        ActionContext actionContext = new(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor());
#pragma warning disable IDE0028 // Simplify collection initialization
        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = ex
        };
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    private static void RunFilter(ExceptionContext context)
    {
        ApiProblemDetailsExceptionFilter filter = new();
        filter.OnException(context);
    }
}
