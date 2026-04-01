using System.Text.Json;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Stable JSON shape for <c>artifacts/release/metadata.json</c> (script: Write-ReleasePackageArtifacts.ps1) — 56R handoff.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ReleasePackageMetadataContractTests
{
    private const string MinimalMetadataV11 = """
{
  "schemaVersion": "1.1",
  "packageKind": "ArchiForge.ReleaseCandidate",
  "application": "ArchiForge.Api",
  "informationalVersion": "1.0.0+deadbeef",
  "assemblyVersion": "1.0.0.0",
  "fileVersion": "1.0.0.0",
  "commitSha": "deadbeef",
  "buildTimestampUtc": "2026-01-01T00:00:00.0000000Z",
  "dotnetSdkVersion": "10.0.100",
  "packagerHost": "ci-runner",
  "apiPublishPathRelative": "artifacts/release/api",
  "uiProductionBuildIncluded": false
}
""";

    [Fact]
    public void Release_metadata_json_v1_1_parses_with_expected_handoff_fields()
    {
        using JsonDocument doc = JsonDocument.Parse(MinimalMetadataV11);
        JsonElement root = doc.RootElement;

        root.GetProperty("schemaVersion").GetString().Should().Be("1.1");
        root.GetProperty("packageKind").GetString().Should().Be("ArchiForge.ReleaseCandidate");
        root.GetProperty("application").GetString().Should().Be("ArchiForge.Api");
        root.GetProperty("informationalVersion").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("assemblyVersion").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("fileVersion").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("commitSha").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("buildTimestampUtc").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("dotnetSdkVersion").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("packagerHost").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("apiPublishPathRelative").GetString().Should().Be("artifacts/release/api");
        root.GetProperty("uiProductionBuildIncluded").GetBoolean().Should().BeFalse();
    }
}
