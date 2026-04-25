namespace ArchLucid.Persistence.BlobStore;

/// <summary>Controls when JSON/large text is written to external storage instead of relying on SQL alone.</summary>
public sealed class ArtifactLargePayloadOptions
{
    public const string SectionName = "ArtifactLargePayload";

    /// <summary>When false, SQL holds full payloads and blob URIs stay null.</summary>
    public bool Enabled
    {
        get;
        set;
    }

    /// <summary>Minimum total UTF-16 length before a golden manifest or bundle payload is offloaded.</summary>
    public int MinimumUtf16LengthToOffload
    {
        get;
        set;
    } = 65536;

    /// <summary>Minimum UTF-16 length for a single artifact <c>Content</c> row before offloading.</summary>
    public int MinimumArtifactContentUtf16LengthToOffload
    {
        get;
        set;
    } = 65536;

    /// <summary>None (no offload), Local (filesystem under <see cref="LocalRootPath" />), AzureBlob.</summary>
    public string BlobProvider
    {
        get;
        set;
    } = "None";

    /// <summary>Root directory for <see cref="BlobProvider" /> Local; default empty uses <c>blob-store</c> under app base.</summary>
    public string LocalRootPath
    {
        get;
        set;
    } = "";

    /// <summary>Service URI, e.g. https://mystorage.blob.core.windows.net</summary>
    public string AzureBlobServiceUri
    {
        get;
        set;
    } = "";
}
