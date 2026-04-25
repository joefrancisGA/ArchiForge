namespace ArchLucid.Api.Tests.TestDtos;

public sealed class CreateRunResponseDto
{
    public RunDto Run
    {
        get;
        set;
    } = new();

    public EvidenceBundleDto EvidenceBundle
    {
        get;
        set;
    } = new();

    public List<AgentTaskDto> Tasks
    {
        get;
        set;
    } = [];
}
