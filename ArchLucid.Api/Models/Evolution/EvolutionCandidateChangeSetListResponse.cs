namespace ArchiForge.Api.Models.Evolution;

public sealed class EvolutionCandidateChangeSetListResponse
{
    public IReadOnlyList<EvolutionCandidateChangeSetResponse> Candidates { get; init; } = [];
}
