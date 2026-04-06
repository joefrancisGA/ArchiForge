using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Findings;

public sealed class ArchitectureFinding
{
    public string FindingId { get; set; } = Guid.NewGuid().ToString("N");
    public AgentType SourceAgent { get; set; }
    public string Severity { get; set; } = "Info";
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> EvidenceRefs { get; set; } = [];
}
