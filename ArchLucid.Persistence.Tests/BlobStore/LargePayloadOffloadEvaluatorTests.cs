namespace ArchLucid.Persistence.Tests.BlobStore;

[Trait("Category", "Unit")]
public sealed class LargePayloadOffloadEvaluatorTests
{
    [SkippableFact]
    public void ShouldOffloadManifestOrBundle_Throws_WhenOptionsNull()
    {
        Action act = () => LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(null!, 1000);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [SkippableFact]
    public void ShouldOffloadManifestOrBundle_ReturnsFalse_WhenDisabled()
    {
        ArtifactLargePayloadOptions o = new() { Enabled = false, BlobProvider = "AzureBlob" };

        bool r = LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(o, 1_000_000);

        r.Should().BeFalse();
    }

    [SkippableFact]
    public void ShouldOffloadManifestOrBundle_ReturnsFalse_WhenBelowMinimum()
    {
        ArtifactLargePayloadOptions o = new()
        {
            Enabled = true, MinimumUtf16LengthToOffload = 100, BlobProvider = "AzureBlob"
        };

        bool r = LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(o, 50);

        r.Should().BeFalse();
    }

    [SkippableFact]
    public void ShouldOffloadManifestOrBundle_ReturnsFalse_WhenProviderNone()
    {
        ArtifactLargePayloadOptions o = new() { Enabled = true, MinimumUtf16LengthToOffload = 1, BlobProvider = "None" };

        bool r = LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(o, 10_000);

        r.Should().BeFalse();
    }

    [SkippableFact]
    public void ShouldOffloadManifestOrBundle_ReturnsTrue_WhenEligible()
    {
        ArtifactLargePayloadOptions o = new() { Enabled = true, MinimumUtf16LengthToOffload = 1, BlobProvider = "Local" };

        bool r = LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(o, 5);

        r.Should().BeTrue();
    }

    [SkippableFact]
    public void ShouldOffloadArtifactContent_MirrorsManifestRules()
    {
        ArtifactLargePayloadOptions o = new() { Enabled = true, MinimumArtifactContentUtf16LengthToOffload = 1, BlobProvider = "Local" };

        bool r = LargePayloadOffloadEvaluator.ShouldOffloadArtifactContent(o, 2);

        r.Should().BeTrue();
    }
}
