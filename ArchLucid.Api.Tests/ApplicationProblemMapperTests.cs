using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Analysis;
using ArchLucid.Core;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Application Problem Mapper.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ApplicationProblemMapperTests
{
    [Fact]
    public void TryMapUnhandledException_ComparisonVerificationFailed_Returns422()
    {
        DriftAnalysisResult drift = new()
        {
            DriftDetected = true,
            Summary = "x"
        };
        ComparisonVerificationFailedException ex = new("verify", drift);
        DefaultHttpContext http = CreateHttpContext("/p", "corr-verify");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(422);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.ComparisonVerificationFailed);
        p.Extensions[ProblemCorrelation.ExtensionKey].Should().Be("corr-verify");
    }

    [Fact]
    public void TryMapUnhandledException_Conflict_Returns409()
    {
        ConflictException ex = new("c");
        DefaultHttpContext http = CreateHttpContext("/p", "corr-409");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.Conflict);
        p.Extensions[ProblemCorrelation.ExtensionKey].Should().Be("corr-409");
    }

    [Fact]
    public void TryMapUnhandledException_RunNotFound_Returns404()
    {
        RunNotFoundException ex = new("missing");
        DefaultHttpContext http = CreateHttpContext("/p", "corr-404");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(404);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.RunNotFound);
    }

    [Fact]
    public void TryMapUnhandledException_LlmTokenQuotaExceeded_Returns429()
    {
        LlmTokenQuotaExceededException ex = new("quota");
        DefaultHttpContext http = CreateHttpContext("/p", "corr-429");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.LlmTokenQuotaExceeded);
        p.Extensions.ContainsKey("retryAfterUtc").Should().BeFalse();
    }

    [Fact]
    public void TryMapUnhandledException_LlmTokenQuotaExceeded_with_retry_includes_extension()
    {
        DateTimeOffset retry = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        LlmTokenQuotaExceededException ex = new("quota", retry);
        DefaultHttpContext http = CreateHttpContext("/p", "corr-429-retry");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, http, out ObjectResult? result);

        mapped.Should().BeTrue();
        MvcProblemDetails p = result!.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Extensions["retryAfterUtc"].Should().Be(retry);
    }

    [Fact]
    public void TryMapUnhandledException_InvalidOperation_Returns400()
    {
        InvalidOperationException ex = new("bad op");
        DefaultHttpContext http = CreateHttpContext("/p", "corr-400");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(400);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.BadRequest);
    }

    [Fact]
    public void TryMapUnhandledException_ArgumentException_Returns400Validation()
    {
        ArgumentException ex = new("arg");
        DefaultHttpContext http = CreateHttpContext("/p", "corr-arg");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(400);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public void TryMapUnhandledException_ArgumentNullException_Returns400Validation()
    {
        ArgumentNullException ex = new("p");
        DefaultHttpContext http = CreateHttpContext("/p", "corr-null");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, http, out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(400);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public void TryMapUnhandledException_UnmappedException_ReturnsFalse()
    {
        DefaultHttpContext http = CreateHttpContext("/p", "corr-none");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(
            new NotSupportedException(),
            http,
            out ObjectResult? result);

        mapped.Should().BeFalse();
        result.Should().BeNull();
    }

    private static DefaultHttpContext CreateHttpContext(string path, string traceIdentifier)
    {
        return new DefaultHttpContext { TraceIdentifier = traceIdentifier, Request = { Path = path } };
    }
}
