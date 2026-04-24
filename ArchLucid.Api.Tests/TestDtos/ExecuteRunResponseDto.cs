namespace ArchLucid.Api.Tests.TestDtos;

public sealed class ExecuteRunResponseDto
{
    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public List<object> Results
    {
        get;
        set;
    } = [];
}
