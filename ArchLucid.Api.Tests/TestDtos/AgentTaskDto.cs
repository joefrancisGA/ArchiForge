namespace ArchLucid.Api.Tests.TestDtos;

public sealed class AgentTaskDto
{
    public string TaskId
    {
        get;
        set;
    } = string.Empty;

    public string AgentType
    {
        get;
        set;
    } = string.Empty;

    public string Objective
    {
        get;
        set;
    } = string.Empty;
}
