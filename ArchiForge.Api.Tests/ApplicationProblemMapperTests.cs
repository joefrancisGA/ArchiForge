using ArchiForge.AgentRuntime;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Application;
using ArchiForge.Application.Analysis;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Tests for Application Problem Mapper.
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

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, "/p", out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(422);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.ComparisonVerificationFailed);
    }

    [Fact]
    public void TryMapUnhandledException_Conflict_Returns409()
    {
        ConflictException ex = new("c");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, "/p", out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.Conflict);
    }

    [Fact]
    public void TryMapUnhandledException_RunNotFound_Returns404()
    {
        RunNotFoundException ex = new("missing");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, "/p", out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(404);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.RunNotFound);
    }

    [Fact]
    public void TryMapUnhandledException_LlmTokenQuotaExceeded_Returns429()
    {
        LlmTokenQuotaExceededException ex = new("quota");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, "/p", out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.LlmTokenQuotaExceeded);
    }

    [Fact]
    public void TryMapUnhandledException_InvalidOperation_Returns400()
    {
        InvalidOperationException ex = new("bad op");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, "/p", out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(400);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.BadRequest);
    }

    [Fact]
    public void TryMapUnhandledException_ArgumentException_Returns400Validation()
    {
        ArgumentException ex = new("arg");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, "/p", out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(400);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public void TryMapUnhandledException_ArgumentNullException_Returns400Validation()
    {
        ArgumentNullException ex = new("p");

        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, "/p", out ObjectResult? result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(400);
        MvcProblemDetails p = result.Value.Should().BeOfType<MvcProblemDetails>().Subject;
        p.Type.Should().Be(ProblemTypes.ValidationFailed);
    }

    [Fact]
    public void TryMapUnhandledException_UnmappedException_ReturnsFalse()
    {
        bool mapped = ApplicationProblemMapper.TryMapUnhandledException(
            new NotSupportedException(),
            "/p",
            out ObjectResult? result);

        mapped.Should().BeFalse();
        result.Should().BeNull();
    }
}
