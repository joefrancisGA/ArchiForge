namespace ArchLucid.Api.Tests.TestDtos;

public sealed class DecisionTraceDto
{
    public string TraceId
    {
        get;
        set;
    } = string.Empty;

    public string EventType
    {
        get;
        set;
    } = string.Empty;

    public string EventDescription
    {
        get;
        set;
    } = string.Empty;
}
