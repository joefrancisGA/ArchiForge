using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Application.Explanation;

/// <inheritdoc cref="IFindingEvidenceChainService" />
public sealed class FindingEvidenceChainService(
    IAuthorityQueryService authorityQuery,
    IScopeContextProvider scopeContextProvider,
    IAgentExecutionTraceRepository agentExecutionTraceRepository) : IFindingEvidenceChainService
{
    private readonly IAuthorityQueryService _authorityQuery =
        authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IAgentExecutionTraceRepository _agentExecutionTraceRepository =
        agentExecutionTraceRepository ?? throw new ArgumentNullException(nameof(agentExecutionTraceRepository));

    /// <inheritdoc />
    public async Task<FindingEvidenceChainResponse?> BuildAsync(
        string runId,
        string findingId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Run id is required.", nameof(runId));
        if (string.IsNullOrWhiteSpace(findingId)) throw new ArgumentException("Finding id is required.", nameof(findingId));

        if (!TryParseRunGuid(runId, out Guid runGuid))
            return null;


        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunDetailDto? detail = await _authorityQuery.GetRunDetailAsync(scope, runGuid, cancellationToken);

        if (detail?.Run is null)
            return null;


        FindingsSnapshot? snapshot = detail.FindingsSnapshot;

        if (snapshot?.Findings is not { Count: > 0 } findings)
            return null;


        Finding? match = findings.FirstOrDefault(f =>
            string.Equals(f.FindingId, findingId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return null;


        IReadOnlyList<AgentExecutionTrace> traces =
            await _agentExecutionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

        List<string> traceIds = traces.Select(t => t.TraceId).Distinct(StringComparer.Ordinal).ToList();

        return new FindingEvidenceChainResponse
        {
            RunId = runId,
            FindingId = match.FindingId,
            ManifestVersion = detail.Run.CurrentManifestVersion,
            FindingsSnapshotId = detail.Run.FindingsSnapshotId,
            ContextSnapshotId = detail.Run.ContextSnapshotId,
            GraphSnapshotId = detail.Run.GraphSnapshotId,
            DecisionTraceId = detail.Run.DecisionTraceId,
            GoldenManifestId = detail.Run.GoldenManifestId,
            RelatedGraphNodeIds = match.RelatedNodeIds.ToList(),
            AgentExecutionTraceIds = traceIds,
        };
    }

    private static bool TryParseRunGuid(string runId, out Guid runGuid)
    {
        if (Guid.TryParseExact(runId, "N", out runGuid))
            return true;


        return Guid.TryParse(runId, out runGuid);
    }
}
