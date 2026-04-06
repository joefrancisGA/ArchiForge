using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Analysis;

public interface IGraphCoverageAnalyzer
{
    TopologyCoverageResult AnalyzeTopology(GraphSnapshot graphSnapshot);

    SecurityCoverageResult AnalyzeSecurity(GraphSnapshot graphSnapshot);

    PolicyCoverageResult AnalyzePolicy(GraphSnapshot graphSnapshot);

    RequirementCoverageResult AnalyzeRequirements(GraphSnapshot graphSnapshot);
}
