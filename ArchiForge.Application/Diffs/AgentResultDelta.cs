using ArchiForge.Contracts.Common;

namespace ArchiForge.Application.Diffs;

public sealed class AgentResultDelta
{
    public AgentType AgentType { get; set; }

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
