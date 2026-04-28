using ArchLucid.Persistence.GoldenManifests;

namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class GoldenManifestPayloadBlobEnvelopeTests
{
    [Fact]
    public void RoundTrip_serializes_and_merges_into_row()
    {
        GoldenManifestPayloadBlobEnvelope original = GoldenManifestPayloadBlobEnvelope.FromSerializedSlices(
            """{"k":"m"}""",
            "[]",
            "{}",
            "{}",
            "{}",
            "{}",
            "{}",
            "[]",
            "[]",
            "[]",
            "[]",
            "{}");

        string json = original.ToJson();
        GoldenManifestPayloadBlobEnvelope? parsed = GoldenManifestPayloadBlobEnvelope.TryDeserialize(json);
        parsed.Should().NotBeNull();
        parsed.MetadataJson.Should().Be("""{"k":"m"}""");

        GoldenManifestStorageRow row = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ManifestId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "r",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
            MetadataJson = "old",
            RequirementsJson = "old",
            TopologyJson = "old",
            SecurityJson = "old",
            ComplianceJson = "old",
            CostJson = "old",
            ConstraintsJson = "old",
            UnresolvedIssuesJson = "old",
            DecisionsJson = "old",
            AssumptionsJson = "old",
            WarningsJson = "old",
            ProvenanceJson = "old",
            ManifestPayloadBlobUri = "https://example/blob"
        };

        GoldenManifestStorageRow merged = GoldenManifestPayloadBlobEnvelope.MergeIntoRow(row, parsed);
        merged.MetadataJson.Should().Be("""{"k":"m"}""");
        merged.ManifestId.Should().Be(row.ManifestId);
        merged.ManifestPayloadBlobUri.Should().Be("https://example/blob");
    }
}
