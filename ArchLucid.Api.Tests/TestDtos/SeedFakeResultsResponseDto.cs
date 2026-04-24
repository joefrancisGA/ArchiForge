namespace ArchLucid.Api.Tests.TestDtos;

public sealed class SeedFakeResultsResponseDto
{
    public string Message
    {
        get;
        set;
    } = string.Empty;

    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public int ResultCount
    {
        get;
        set;
    }
}
