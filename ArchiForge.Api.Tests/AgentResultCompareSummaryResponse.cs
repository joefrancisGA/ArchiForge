namespace ArchiForge.Api.Tests;

public sealed class AgentResultCompareSummaryResponse
{
    public string Format { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public AgentResultDiffDto Diff { get; set; } = new();
}

public sealed class AgentResultDiffDto
{
    public string LeftRunId { get; set; } = string.Empty;

    public string RightRunId { get; set; } = string.Empty;

    public List<AgentResultDeltaDto> AgentDeltas { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}

public sealed class AgentResultDeltaDto
{
    public string AgentType { get; set; } = string.Empty;

    public bool LeftExists { get; set; }

    public bool RightExists { get; set; }

    public List<string> AddedClaims { get; set; } = [];

    public List<string> RemovedClaims { get; set; } = [];

    public List<string> AddedEvidenceRefs { get; set; } = [];

    public List<string> RemovedEvidenceRefs { get; set; } = [];

    public List<string> AddedFindings { get; set; } = [];

    public List<string> RemovedFindings { get; set; } = [];

    public List<string> AddedRequiredControls { get; set; } = [];

    public List<string> RemovedRequiredControls { get; set; } = [];

    public List<string> AddedWarnings { get; set; } = [];

    public List<string> RemovedWarnings { get; set; } = [];

    public double? LeftConfidence { get; set; }

    public double? RightConfidence { get; set; }
}
