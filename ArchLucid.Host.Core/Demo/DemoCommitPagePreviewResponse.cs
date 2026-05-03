using ArchLucid.Core.Explanation;

namespace ArchLucid.Host.Core.Demo;

/// <summary>
/// Single bundled JSON for <c>GET /v1/demo/preview</c> — everything the marketing commit-page needs without further API calls.
/// </summary>
public sealed class DemoCommitPagePreviewResponse
{
    public DateTimeOffset GeneratedUtc
    {
        get;
        init;
    }

    public bool IsDemoData
    {
        get;
        init;
    } = true;

    public string DemoStatusMessage
    {
        get;
        init;
    } = "demo tenant — replace before publishing";

    public required DemoPreviewRun Run
    {
        get;
        init;
    }

    public required DemoPreviewManifestSummary Manifest
    {
        get;
        init;
    }

    public required DemoPreviewAuthorityChain AuthorityChain
    {
        get;
        init;
    }

    public required IReadOnlyList<DemoPreviewArtifact> Artifacts
    {
        get;
        init;
    }

    public required IReadOnlyList<DemoPreviewTimelineItem> PipelineTimeline
    {
        get;
        init;
    }

    public required RunExplanationSummary RunExplanation
    {
        get;
        init;
    }
}
