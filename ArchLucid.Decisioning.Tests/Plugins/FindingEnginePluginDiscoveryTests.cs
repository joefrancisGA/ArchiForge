using ArchLucid.Decisioning.Plugins;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Decisioning.Tests.Plugins;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class FindingEnginePluginDiscoveryTests
{
    [Fact]
    public void Discover_null_or_whitespace_directory_returns_empty()
    {
        IReadOnlyList<Type> a = FindingEnginePluginDiscovery.Discover(null, NullLogger.Instance);
        IReadOnlyList<Type> b = FindingEnginePluginDiscovery.Discover("   ", NullLogger.Instance);

        a.Should().BeEmpty();
        b.Should().BeEmpty();
    }

    [Fact]
    public void Discover_nonexistent_directory_returns_empty()
    {
        string path = Path.Combine(Path.GetTempPath(), "archlucid-plugin-missing-" + Guid.NewGuid().ToString("N"));

        IReadOnlyList<Type> found = FindingEnginePluginDiscovery.Discover(path, NullLogger.Instance);

        found.Should().BeEmpty();
    }
}
