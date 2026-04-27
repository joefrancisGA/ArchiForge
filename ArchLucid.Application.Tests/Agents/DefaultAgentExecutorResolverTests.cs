using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Application.Agents;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Agents;

/// <summary>
/// Unit tests for <see cref="DefaultAgentExecutorResolver"/>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DefaultAgentExecutorResolverTests
{
    [Theory]
    [InlineData(ExecutionModes.Current)]
    [InlineData(ExecutionModes.Deterministic)]
    [InlineData(ExecutionModes.Replay)]
    [InlineData("current")]
    [InlineData("REPLAY")]
    public void Resolve_known_mode_returns_injected_executor(string mode)
    {
        Mock<IAgentExecutor> executor = new();
        DefaultAgentExecutorResolver sut = new(executor.Object);

        IAgentExecutor resolved = sut.Resolve(mode);

        resolved.Should().BeSameAs(executor.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_null_or_whitespace_throws(string? mode)
    {
        Mock<IAgentExecutor> executor = new();
        DefaultAgentExecutorResolver sut = new(executor.Object);

        Action act = () => _ = sut.Resolve(mode!);

        act.Should().Throw<ArgumentException>().WithParameterName("executionMode");
    }

    [Fact]
    public void Resolve_unknown_mode_throws_with_parameter_name()
    {
        Mock<IAgentExecutor> executor = new();
        DefaultAgentExecutorResolver sut = new(executor.Object);

        Action act = () => _ = sut.Resolve("UnknownMode");

        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("executionMode")
            .WithMessage("*Unknown execution mode*UnknownMode*");
    }
}
