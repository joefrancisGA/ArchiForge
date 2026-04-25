namespace ArchLucid.Api.Tests.TestDtos;

public sealed class GetRunResponseDto
{
    public RunDto Run
    {
        get;
        set;
    } = new();

    public List<AgentTaskDto> Tasks
    {
        get;
        set;
    } = [];

    public List<object> Results
    {
        get;
        set;
    } = [];
}
