namespace ArchiForge.Persistence.BlobStore;

internal static class LargePayloadOffloadEvaluator
{
    internal static bool ShouldOffloadManifestOrBundle(ArtifactLargePayloadOptions options, int utf16Length)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
            return false;

        if (utf16Length < Math.Max(0, options.MinimumUtf16LengthToOffload))
            return false;

        return !string.Equals(options.BlobProvider, "None", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ShouldOffloadArtifactContent(ArtifactLargePayloadOptions options, int utf16Length)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
            return false;

        if (utf16Length < Math.Max(0, options.MinimumArtifactContentUtf16LengthToOffload))
            return false;

        return !string.Equals(options.BlobProvider, "None", StringComparison.OrdinalIgnoreCase);
    }
}
