using ArchLucid.Api.Models;
using ArchLucid.Application;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Models;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ReplayRunRequestExecutionModeDefaultTests
{
    [Fact]
    public void ExecutionMode_defaults_to_ExecutionModes_Current()
    {
        ReplayRunRequest sut = new();

        sut.ExecutionMode.Should().Be(ExecutionModes.Current);
    }
}
