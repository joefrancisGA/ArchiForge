using ArchiForgeFindingEngine;

namespace ArchiForgeFindingEngine.Tests;

public sealed class SampleFindingTests
{
    [Fact]
    public void Describe_returns_marker()
    {
        string s = SampleFinding.Describe();

        Assert.Equal("archiforge-finding-engine", s);
    }
}
