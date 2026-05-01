using ArchLucid.Core.Scoping;

namespace ArchLucid.Application.Audit;

/// <summary>Loads chronological audit events for one authority run (defense-in-depth: verifies run exists in scope first).</summary>
public interface IRunPipelineAuditTimelineService
{
    /// <summary>Returns timeline items oldest-first, or <see langword="null" /> when the run is missing in scope.</summary>
    Task<IReadOnlyList<RunPipelineTimelineItemDto>?> GetTimelineAsync(
        ScopeContext scope,
        Guid runId,
        CancellationToken cancellationToken);
}
