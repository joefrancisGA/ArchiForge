using ArchiForge.Decisioning.Compliance.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Compliance.Evaluators;

public interface IComplianceEvaluator
{
    ComplianceEvaluationResult Evaluate(
        GraphSnapshot graphSnapshot,
        ComplianceRulePack rulePack);
}
