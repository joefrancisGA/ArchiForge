using ArchiForge.Contracts.Evolution;

namespace ArchiForge.Api.Models.Evolution;

internal static class EvolutionCandidateChangeSetResponseMapper
{
    internal static EvolutionCandidateChangeSetResponse ToResponse(this EvolutionCandidateChangeSetRecord record) =>
        new()
        {
            CandidateChangeSetId = record.CandidateChangeSetId,
            SourcePlanId = record.SourcePlanId,
            Status = record.Status,
            Title = record.Title,
            Summary = record.Summary,
            DerivationRuleVersion = record.DerivationRuleVersion,
            CreatedUtc = record.CreatedUtc,
            CreatedByUserId = record.CreatedByUserId,
        };

    internal static EvolutionSimulationRunResponse ToResponse(this EvolutionSimulationRunRecord record) =>
        new()
        {
            SimulationRunId = record.SimulationRunId,
            BaselineArchitectureRunId = record.BaselineArchitectureRunId,
            EvaluationMode = record.EvaluationMode,
            OutcomeJson = record.OutcomeJson,
            WarningsJson = record.WarningsJson,
            CompletedUtc = record.CompletedUtc,
            IsShadowOnly = record.IsShadowOnly,
        };
}
