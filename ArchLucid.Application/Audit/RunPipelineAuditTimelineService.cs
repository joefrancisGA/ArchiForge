using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Application.Audit;
/// <inheritdoc cref = "IRunPipelineAuditTimelineService"/>
public sealed class RunPipelineAuditTimelineService(IAuthorityQueryService authorityQuery, IAuditRepository auditRepository) : IRunPipelineAuditTimelineService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(authorityQuery, auditRepository);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Queries.IAuthorityQueryService authorityQuery, ArchLucid.Persistence.Audit.IAuditRepository auditRepository)
    {
        ArgumentNullException.ThrowIfNull(authorityQuery);
        ArgumentNullException.ThrowIfNull(auditRepository);
        return (byte)0;
    }

    private readonly IAuditRepository _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
    private readonly IAuthorityQueryService _authorityQuery = authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArchLucid.Application.Audit.RunPipelineTimelineItemDto>?> GetTimelineAsync(ScopeContext scope, Guid runId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        RunSummaryDto? run = await _authorityQuery.GetRunSummaryAsync(scope, runId, cancellationToken);
        if (run is null)
            return null;
        AuditEventFilter filter = new()
        {
            RunId = runId,
            Take = 200
        };
        IReadOnlyList<AuditEvent> rows = await _auditRepository.GetFilteredAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, filter, cancellationToken);
        List<AuditEvent> ordered = rows.OrderBy(e => e.OccurredUtc).ThenBy(e => e.EventId).ToList();
        List<RunPipelineTimelineItemDto> items = ordered.Select(e => new RunPipelineTimelineItemDto(e.EventId, e.OccurredUtc, e.EventType, e.ActorUserName, e.CorrelationId)).ToList();
        return items;
    }
}