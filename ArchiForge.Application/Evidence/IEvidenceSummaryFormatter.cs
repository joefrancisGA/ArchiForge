using ArchiForge.Contracts.Agents;

namespace ArchiForge.Application.Evidence;

public interface IEvidenceSummaryFormatter
{
    string FormatMarkdown(AgentEvidencePackage evidence);
}
