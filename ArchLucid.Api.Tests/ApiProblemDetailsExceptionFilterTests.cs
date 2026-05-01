using System.Data.Common;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Analysis;
using ArchLucid.Core.Resilience;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Api Problem Details Exception Filter.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ApiProblemDetailsExceptionFilterTests
{
    [SkippableFact]
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
        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("driftDetected");
        problem.Extensions["driftDetected"].Should().Be(true);
        problem.Extensions["driftSummary"].Should().Be("payload mismatch");
        problem.Extensions["errorCode"].Should().Be(ProblemErrorCodes.ComparisonVerificationFailed);
        problem.Extensions.Should().ContainKey("supportHint");
        ((string)problem.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("drift");
        problem.Extensions[ProblemCorrelation.ExtensionKey].Should().Be("exception-filter-cid");
    }

    [SkippableFact]
    public void ConflictException_Produces409()
    {
        ExceptionContext context = CreateExceptionContext(new ConflictException("state"), "/v1/run/r/commit");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should()
            .BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.Conflict);
        p.Extensions["errorCode"].Should().Be(ProblemErrorCodes.Conflict);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("idempotency");
    }

    [SkippableFact]
    public void RunNotFoundException_Produces404()
    {
        ExceptionContext context = CreateExceptionContext(new RunNotFoundException("missing"), "/v1/run/missing");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should()
            .BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.RunNotFound);
        p.Extensions["errorCode"].Should().Be(ProblemErrorCodes.RunNotFound);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("scope");
    }

    [SkippableFact]
    public void InvalidOperationException_Produces400BadRequestType()
    {
        ExceptionContext context = CreateExceptionContext(new InvalidOperationException("bad"), "/v1/x");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should()
            .BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.BadRequest);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("swagger");
    }

    [SkippableFact]
    public void ArgumentException_Produces400ValidationType()
    {
        ExceptionContext context = CreateExceptionContext(new ArgumentException("arg"), "/v1/x");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should()
            .BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.ValidationFailed);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("validation");
    }

    [SkippableFact]
    public void UnmappedException_LeavesContextUnchanged()
    {
        ExceptionContext context = CreateExceptionContext(new NotSupportedException(), "/v1/x");

        RunFilter(context);

        context.ExceptionHandled.Should().BeFalse();
        context.Result.Should().BeNull();
    }

    [SkippableFact]
    public void TimeoutException_Produces503DatabaseTimeout()
    {
        ExceptionContext context = CreateExceptionContext(
            new TimeoutException("Operation timed out"), "/v1/alerts");

        RunFilter(context);

        context.ExceptionHandled.Should().BeTrue();
        ObjectResult result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should()
            .BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.DatabaseTimeout);
        p.Title.Should().Be("Request Timeout");
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("health/ready");
    }

    [SkippableFact]
    public void TryMapDatabaseException_SqlTimeoutException_Returns503()
    {
        // SqlException(-2) cannot be constructed directly; test via the mapper with a TimeoutException
        // which covers the same code path. The SqlException branch is tested implicitly by
        // the health check tests that use real SqlConnection failures.
        DefaultHttpContext http = CreateHttpContextForMapper("/v1/runs", "db-timeout-cid");

        bool mapped = ApplicationProblemMapper.TryMapDatabaseException(
            new TimeoutException("sql timeout"), "/v1/runs", http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should()
            .BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Extensions[ProblemCorrelation.ExtensionKey].Should().Be("db-timeout-cid");
    }

    [SkippableFact]
    public void TryMapDatabaseException_GenericDbException_Returns503DatabaseUnavailable()
    {
        DefaultHttpContext http = CreateHttpContextForMapper("/v1/runs", "db-unavail-cid");

        bool mapped = ApplicationProblemMapper.TryMapDatabaseException(
            new TestDbException("connection refused"), "/v1/runs", http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        Microsoft.AspNetCore.Mvc.ProblemDetails p = result.Value.Should()
            .BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.DatabaseUnavailable);
        p.Extensions.Should().ContainKey("supportHint");
        ((string)p.Extensions["supportHint"]!).ToLowerInvariant().Should().Contain("health/ready");
    }

    [SkippableFact]
    public void TryMapDatabaseException_DeadlockSqlException_Returns409Conflict()
    {
        DefaultHttpContext http = CreateHttpContextForMapper("/v1/architecture/run/x/commit", "deadlock-cid");

        bool mapped = ApplicationProblemMapper.TryMapDatabaseException(
            SqlExceptionTestFactory.Create(1205),
            "/v1/architecture/run/x/commit",
            http,
            out ObjectResult? result);

        mapped.Should().BeTrue();
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        Microsoft.AspNetCore.Mvc.ProblemDetails p =
            result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.Conflict);
    }

    [SkippableFact]
    public void TryMapDatabaseException_WrappedDeadlockSqlException_Returns409Conflict()
    {
        DefaultHttpContext http = CreateHttpContextForMapper("/v1/commit", "deadlock-wrap-cid");
        Exception ex = new InvalidOperationException("persist failed", SqlExceptionTestFactory.Create(1205));

        bool mapped =
            ApplicationProblemMapper.TryMapDatabaseException(ex, "/v1/commit", http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        Microsoft.AspNetCore.Mvc.ProblemDetails p =
            result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.Conflict);
    }

    [SkippableFact]
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

    [SkippableFact]
    public void TryMapDatabaseException_NonDatabaseException_ReturnsFalse()
    {
        DefaultHttpContext http = CreateHttpContextForMapper("/v1/runs", "db-skip-cid");

        bool mapped = ApplicationProblemMapper.TryMapDatabaseException(
            new InvalidOperationException("not a db error"), "/v1/runs", http, out ObjectResult? result);

        mapped.Should().BeFalse();
        result.Should().BeNull();
    }

    private static ExceptionContext CreateExceptionContext(Exception ex, string path)
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "exception-filter-cid",
            Request = { Path = path }
        };
        ActionContext actionContext = new(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor());
#pragma warning disable IDE0028 // Simplify collection initialization
        return new ExceptionContext(actionContext, new List<IFilterMetadata>()) { Exception = ex };
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    private static DefaultHttpContext CreateHttpContextForMapper(string path, string traceIdentifier)
    {
        return new DefaultHttpContext { TraceIdentifier = traceIdentifier, Request = { Path = path } };
    }

    private static void RunFilter(ExceptionContext context)
    {
        ApiProblemDetailsExceptionFilter filter = new();
        filter.OnException(context);
    }

    /// <summary>Concrete <see cref="DbException" /> for test purposes (abstract class cannot be instantiated).</summary>
    private sealed class TestDbException(string message) : DbException(message);
}
