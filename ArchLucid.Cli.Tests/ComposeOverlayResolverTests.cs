using ArchLucid.Cli.Real;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

public sealed class ComposeOverlayResolverTests
{
    [Fact]
    public void Resolve_WhenNotReal_returnsDemoOnly()
    {
        IReadOnlyList<string> overlays = ComposeOverlayResolver.Resolve(false);

        overlays.Should().Equal("docker-compose.demo.yml");
    }

    [Fact]
    public void Resolve_WhenReal_returnsDemoThenRealAoai()
    {
        IReadOnlyList<string> overlays = ComposeOverlayResolver.Resolve(true);

        overlays.Should().Equal("docker-compose.demo.yml", "docker-compose.real-aoai.yml");
    }
}
