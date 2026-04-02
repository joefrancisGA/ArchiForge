using System.Reflection;

using ArchiForge.Core.Diagnostics;

using FluentAssertions;

namespace ArchiForge.Core.Tests.Diagnostics;

[Trait("Category", "Unit")]
public sealed class BuildProvenanceTests
{
    [Fact]
    public void FromAssembly_Throws_WhenAssemblyIsNull()
    {
        Action act = () => BuildProvenance.FromAssembly(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromAssembly_Resolves_FromCoreAssembly()
    {
        Assembly core = typeof(BuildProvenance).Assembly;

        BuildProvenance p = BuildProvenance.FromAssembly(core);

        p.AssemblyVersion.Should().NotBeNullOrWhiteSpace();
        p.InformationalVersion.Should().NotBeNullOrWhiteSpace();
        p.RuntimeFrameworkDescription.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("1.0.0", null)]
    [InlineData("1.0.0+", null)]
    [InlineData("1.0.0+abc", "abc")]
    [InlineData("1.0.0-preview+deadbeef", "deadbeef")]
    public void ParseCommitSha_MatchesInformationalSuffixRules(string informational, string? expectedSha)
    {
        string? sha = BuildProvenance.ParseCommitSha(informational);

        sha.Should().Be(expectedSha);
    }
}
