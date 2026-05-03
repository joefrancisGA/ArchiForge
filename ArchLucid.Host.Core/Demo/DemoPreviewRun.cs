namespace ArchLucid.Host.Core.Demo;

/// <summary>Flat run header for marketing commit-page preview.</summary>
public sealed class DemoPreviewRun
{
    public required string RunId
    {
        get;
        init;
    }

    public required string ProjectId
    {
        get;
        init;
    }

    public string? Description
    {
        get;
        init;
    }

    public required DateTime CreatedUtc
    {
        get;
        init;
    }
}
