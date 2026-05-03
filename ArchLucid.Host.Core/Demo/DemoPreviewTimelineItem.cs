namespace ArchLucid.Host.Core.Demo;

/// <summary>One pipeline timeline row for marketing preview.</summary>
public sealed class DemoPreviewTimelineItem
{
    public required string EventId
    {
        get;
        init;
    }

    public required DateTime OccurredUtc
    {
        get;
        init;
    }

    public required string EventType
    {
        get;
        init;
    }

    public required string ActorUserName
    {
        get;
        init;
    }

    public string? CorrelationId
    {
        get;
        init;
    }
}
