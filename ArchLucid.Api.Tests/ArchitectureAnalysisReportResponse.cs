using ArchLucid.Api.Tests.TestDtos;

namespace ArchLucid.Api.Tests;

public sealed class ArchitectureAnalysisReportResponse
{
    public ArchitectureAnalysisReportDto Report
    {
        get;
        set;
    } = new();
}

public sealed class ArchitectureAnalysisReportDto
{
    public RunDto Run
    {
        get;
        set;
    } = new();

    public AgentEvidencePackageDto? Evidence
    {
        get;
        set;
    }

    public List<AgentExecutionTraceDto> ExecutionTraces
    {
        get;
        set;
    } = [];

    public ManifestDto? Manifest
    {
        get;
        set;
    }

    public string? Diagram
    {
        get;
        set;
    }

    public string? Summary
    {
        get;
        set;
    }

    public DeterminismCheckResultDto? Determinism
    {
        get;
        set;
    }

    public ManifestDiffDto? ManifestDiff
    {
        get;
        set;
    }

    public AgentResultDiffDto? AgentResultDiff
    {
        get;
        set;
    }

    public List<string> Warnings
    {
        get;
        set;
    } = [];
}

public sealed class AgentExecutionTraceDto
{
    public string TraceId
    {
        get;
        set;
    } = string.Empty;

    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public string TaskId
    {
        get;
        set;
    } = string.Empty;

    public string SystemPrompt
    {
        get;
        set;
    } = string.Empty;

    public string UserPrompt
    {
        get;
        set;
    } = string.Empty;

    public string RawResponse
    {
        get;
        set;
    } = string.Empty;
}
