using ArchLucid.Persistence.ArtifactBundles;
using ArchLucid.Persistence.BlobStore;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.BlobStore;

public sealed class ArtifactBundlePayloadBlobEnvelopeTests
{
    [Fact]
    public void SumUtf16Length_adds_lengths()
    {
        int n = ArtifactBundlePayloadBlobEnvelope.SumUtf16Length("ab", "cde");

        n.Should().Be(5);
    }

    [Fact]
    public void ToJson_round_trips_via_TryDeserialize()
    {
        ArtifactBundlePayloadBlobEnvelope original =
            ArtifactBundlePayloadBlobEnvelope.FromJsonPair("[1]", "{\"t\":true}");
        string json = original.ToJson();

        ArtifactBundlePayloadBlobEnvelope? back = ArtifactBundlePayloadBlobEnvelope.TryDeserialize(json);

        back.Should().NotBeNull();
        back.ArtifactsJson.Should().Be("[1]");
        back.TraceJson.Should().Be("{\"t\":true}");
        back.SchemaVersion.Should().Be(ArtifactBundlePayloadBlobEnvelope.CurrentSchemaVersion);
    }

    [Fact]
    public void TryDeserialize_null_or_whitespace_returns_null()
    {
        ArtifactBundlePayloadBlobEnvelope.TryDeserialize(string.Empty).Should().BeNull();
        ArtifactBundlePayloadBlobEnvelope.TryDeserialize("   ").Should().BeNull();
    }

    [Fact]
    public void TryDeserialize_invalid_json_returns_null()
    {
        ArtifactBundlePayloadBlobEnvelope.TryDeserialize("{").Should().BeNull();
    }

    [Fact]
    public void MergeIntoRow_copies_json_fields()
    {
        Guid runId = Guid.NewGuid();
        ArtifactBundleStorageRow row = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            BundleId = Guid.NewGuid(),
            RunId = runId,
            ManifestId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ArtifactsJson = "old-a",
            TraceJson = "old-t",
            BundlePayloadBlobUri = "https://blob"
        };

        ArtifactBundlePayloadBlobEnvelope env = ArtifactBundlePayloadBlobEnvelope.FromJsonPair("new-a", "new-t");

        ArtifactBundleStorageRow merged = ArtifactBundlePayloadBlobEnvelope.MergeIntoRow(row, env);

        merged.ArtifactsJson.Should().Be("new-a");
        merged.TraceJson.Should().Be("new-t");
        merged.BundlePayloadBlobUri.Should().Be("https://blob");
        merged.TenantId.Should().Be(row.TenantId);
        merged.RunId.Should().Be(runId);
    }
}
