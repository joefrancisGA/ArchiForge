using FluentAssertions;

namespace ArchiForge.Api.Tests;

public static class ManifestAssertions
{
    public static void MatchSummary(
        ManifestDto manifest, 
        ExpectedManifestSummary expected)
    {
        manifest.SystemName.Should().Be(expected.SystemName);

        manifest.Services
            .Select(s => s.ServiceName)
            .Should()
            .Contain(expected.Services);

        manifest.Datastores
            .Select(d => d.DatastoreName)
            .Should()
            .Contain(expected.Datastores);

        manifest.Governance.RequiredControls
            .Should()
            .Contain(expected.RequiredControls);
    }
}