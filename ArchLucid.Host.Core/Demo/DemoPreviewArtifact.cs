namespace ArchLucid.Host.Core.Demo;

/// <summary>Descriptor row for read-only artifact table (no download URLs).</summary>
public sealed class DemoPreviewArtifact
{
    public required string ArtifactId
    {
        get;
        init;
    }

    public required string ArtifactType
    {
        get;
        init;
    }

    public required string Name
    {
        get;
        init;
    }

    public required string Format
    {
        get;
        init;
    }

    public required DateTime CreatedUtc
    {
        get;
        init;
    }

    public required string ContentHash
    {
        get;
        init;
    }
}
