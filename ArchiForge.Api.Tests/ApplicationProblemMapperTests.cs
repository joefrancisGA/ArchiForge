using ArchiForge.Api.ProblemDetails;
using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ApplicationProblemMapperTests
{
    [Theory]
    [InlineData("Run 'x' was not found.")]
    [InlineData("Task 't' was not found for run 'r'.")]
    [InlineData("ArchitectureRequest 'req' for run 'r' was not found.")]
    public void InferNotFoundProblemType_run_scoped_messages_use_run_not_found(string message) =>
        ApplicationProblemMapper.InferNotFoundProblemType(message).Should().Be(ProblemTypes.RunNotFound);

    [Fact]
    public void InferNotFoundProblemType_generic_resource_uses_resource_not_found() =>
        ApplicationProblemMapper.InferNotFoundProblemType("Export record 'e' was not found.")
            .Should().Be(ProblemTypes.ResourceNotFound);
}
