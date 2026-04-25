using ArchLucid.Api.Tests.TestDtos;

namespace ArchLucid.Api.Tests;

public sealed class ReplayRunResponseDto
{
    public string OriginalRunId
    {
        get;
        set;
    } = string.Empty;

    public string ReplayRunId
    {
        get;
        set;
    } = string.Empty;

    public string ExecutionMode
    {
        get;
        set;
    } = string.Empty;

    public List<object> Results
    {
        get;
        set;
    } = [];

    public ManifestDto? Manifest
    {
        get;
        set;
    }

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
