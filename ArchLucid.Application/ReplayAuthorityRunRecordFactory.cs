using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Application;

/// <summary>
///     Builds <see cref="RunRecord" /> rows for replay runs so <c>dbo.Runs</c> is the only persisted run header.
/// </summary>
internal static class ReplayAuthorityRunRecordFactory
{
    /// <summary>
    ///     Creates the authority row for a new replay run id. When the source run exists in <c>dbo.Runs</c>,
    ///     tenant/workspace/scope are cloned from that row; otherwise the HTTP (or job) <paramref name="callScope" />
    ///     applies. <see cref="RunRecord.ProjectId" /> follows the coordinator slug convention (request
    ///     <c>SystemName</c>) when the source row has no project id.
    /// </summary>
    public static RunRecord CreateForReplay(
        Guid replayRunId,
        ScopeContext callScope,
        RunRecord? sourceAuthorityRun,
        ArchitectureRequest request)
    {
        ArgumentNullException.ThrowIfNull(callScope);
        ArgumentNullException.ThrowIfNull(request);

        DateTime createdUtc = DateTime.UtcNow;

        if (sourceAuthorityRun is not null)

            return new RunRecord
            {
                TenantId = sourceAuthorityRun.TenantId,
                WorkspaceId = sourceAuthorityRun.WorkspaceId,
                ScopeProjectId = sourceAuthorityRun.ScopeProjectId,
                RunId = replayRunId,
                ProjectId = string.IsNullOrWhiteSpace(sourceAuthorityRun.ProjectId)
                    ? request.SystemName
                    : sourceAuthorityRun.ProjectId,
                Description = sourceAuthorityRun.Description,
                CreatedUtc = createdUtc,
                ArchitectureRequestId = request.RequestId
            };

        return new RunRecord
        {
            TenantId = callScope.TenantId,
            WorkspaceId = callScope.WorkspaceId,
            ScopeProjectId = callScope.ProjectId,
            RunId = replayRunId,
            ProjectId = request.SystemName,
            CreatedUtc = createdUtc,
            ArchitectureRequestId = request.RequestId
        };
    }
}
