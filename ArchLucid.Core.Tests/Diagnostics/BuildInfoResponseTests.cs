using ArchLucid.Core.Diagnostics;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Category", "Unit")]
public sealed class BuildInfoResponseTests
{
    [Fact]
    public void FromProvenance_Throws_WhenProvenanceIsNull()
    {
        Action act = () => BuildInfoResponse.FromProvenance(null!, "app", "env");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromProvenance_MapsFields_FromProvenanceAndNames()
    {
        BuildProvenance p = new(
            "1.2.3+sha",
            "1.2.0.0",
            "1.2.3.4",
            ".NET Test",
            "sha");

        BuildInfoResponse r = BuildInfoResponse.FromProvenance(p, "ArchLucid.Api", "Staging");

        r.Application.Should().Be("ArchLucid.Api");
        r.Environment.Should().Be("Staging");
        r.InformationalVersion.Should().Be("1.2.3+sha");
        r.AssemblyVersion.Should().Be("1.2.0.0");
        r.FileVersion.Should().Be("1.2.3.4");
        r.CommitSha.Should().Be("sha");
        r.RuntimeFramework.Should().Be(".NET Test");
    }
}
