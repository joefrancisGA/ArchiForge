using ArchiForge.Api.ProblemDetails;
using ArchiForge.Application;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ApplicationProblemMapperTests
{
    [Theory]
    [InlineData("Something went wrong.")]
    [InlineData("Run 'x' was not found.")]   // "not found" in message must NOT produce 404 anymore
    [InlineData("No tasks found for run 'r'.")]
    public void MapInvalidOperation_always_returns_400_regardless_of_message(string message)
    {
        var result = ApplicationProblemMapper.MapInvalidOperation(
            new InvalidOperationException(message),
            instance: null,
            badRequestProblemType: ProblemTypes.BadRequest,
            notFoundTypeOverride: null);

        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void TryMapUnhandledException_RunNotFoundException_returns_404_run_not_found()
    {
        var ex = new RunNotFoundException("run-123");

        var mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, instance: null, out var result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = result.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        problem!.Type.Should().Be(ProblemTypes.RunNotFound);
    }

    [Fact]
    public void TryMapUnhandledException_ConflictException_returns_409()
    {
        var ex = new ConflictException("Run is already committed.");

        var mapped = ApplicationProblemMapper.TryMapUnhandledException(ex, instance: null, out var result);

        mapped.Should().BeTrue();
        result!.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }
}
