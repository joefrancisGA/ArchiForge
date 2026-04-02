using ArchiForge.Core.Diagnostics;

using FluentAssertions;

namespace ArchiForge.Core.Tests.Diagnostics;

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
            InformationalVersion: "1.2.3+sha",
            AssemblyVersion: "1.2.0.0",
            FileVersion: "1.2.3.4",
            RuntimeFrameworkDescription: ".NET Test",
            CommitSha: "sha");

        BuildInfoResponse r = BuildInfoResponse.FromProvenance(p, "ArchiForge.Api", "Staging");

        r.Application.Should().Be("ArchiForge.Api");
        r.Environment.Should().Be("Staging");
        r.InformationalVersion.Should().Be("1.2.3+sha");
        r.AssemblyVersion.Should().Be("1.2.0.0");
        r.FileVersion.Should().Be("1.2.3.4");
        r.CommitSha.Should().Be("sha");
        r.RuntimeFramework.Should().Be(".NET Test");
    }
}
