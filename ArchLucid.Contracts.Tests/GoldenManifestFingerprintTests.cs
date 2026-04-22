using ArchLucid.Contracts.Manifest;

using FluentAssertions;

namespace ArchLucid.Contracts.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class GoldenManifestFingerprintTests
{
    [Fact]
    public void ComputeSha256Hex_is_deterministic_for_equivalent_manifest()
    {
        DateTime createdUtc = new(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc);

        GoldenManifest a = new()
        {
            RunId = "run-a",
            SystemName = "Sys",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new(),
            Metadata = new() { ManifestVersion = "v1-test", CreatedUtc = createdUtc },
        };

        GoldenManifest b = new()
        {
            RunId = "run-a",
            SystemName = "Sys",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new(),
            Metadata = new() { ManifestVersion = "v1-test", CreatedUtc = createdUtc },
        };

        string ha = GoldenManifestFingerprint.ComputeSha256Hex(a);
        string hb = GoldenManifestFingerprint.ComputeSha256Hex(b);

        ha.Should().Be(hb);
        ha.Length.Should().Be(64);
    }
}
