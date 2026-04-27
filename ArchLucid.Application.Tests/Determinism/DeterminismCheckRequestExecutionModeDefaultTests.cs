using ArchLucid.Application;
using ArchLucid.Application.Determinism;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Determinism;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DeterminismCheckRequestExecutionModeDefaultTests
{
    [Fact]
    public void ExecutionMode_defaults_to_ExecutionModes_Current()
    {
        DeterminismCheckRequest sut = new();

        sut.ExecutionMode.Should().Be(ExecutionModes.Current);
    }
}
