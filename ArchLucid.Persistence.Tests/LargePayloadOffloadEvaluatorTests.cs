namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class LargePayloadOffloadEvaluatorTests
{
    [Fact]
    public void ShouldOffloadManifestOrBundle_false_when_disabled()
    {
        ArtifactLargePayloadOptions o = new()
        {
            Enabled = false, MinimumUtf16LengthToOffload = 1, BlobProvider = "Local"
        };

        LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(o, 999_999).Should().BeFalse();
    }

    [Fact]
    public void ShouldOffloadManifestOrBundle_false_when_provider_none()
    {
        ArtifactLargePayloadOptions o = new()
        {
            Enabled = true, MinimumUtf16LengthToOffload = 10, BlobProvider = "None"
        };

        LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(o, 100).Should().BeFalse();
    }

    [Fact]
    public void ShouldOffloadManifestOrBundle_true_when_over_threshold_and_local()
    {
        ArtifactLargePayloadOptions o = new()
        {
            Enabled = true, MinimumUtf16LengthToOffload = 10, BlobProvider = "Local"
        };

        LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(o, 10).Should().BeTrue();
    }
}
