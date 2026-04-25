namespace ArchLucid.Api.Tests.TestDtos;

public sealed class CommitRunResponseDto
{
    public ManifestDto Manifest
    {
        get;
        set;
    } = new();

    public List<DecisionTraceDto> DecisionTraces
    {
        get;
        set;
    } = [];

    public List<string> Warnings
    {
        get;
        set;
    } = [];
}
