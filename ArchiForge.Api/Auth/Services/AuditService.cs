using System.Diagnostics;
using System.Security.Claims;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Audit;
using Microsoft.AspNetCore.Http;

namespace ArchiForge.Api.Auth.Services;

/// <summary>
/// Fills actor, scope, default DataJson, and correlation id before appending to the audit store.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly IAuditRepository _repo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScopeContextProvider _scopeProvider;

    public AuditService(
        IAuditRepository repo,
        IHttpContextAccessor httpContextAccessor,
        IScopeContextProvider scopeProvider)
    {
        _repo = repo;
        _httpContextAccessor = httpContextAccessor;
        _scopeProvider = scopeProvider;
    }

    public async Task LogAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        var http = _httpContextAccessor.HttpContext;
        var user = http?.User;
        var scope = _scopeProvider.GetCurrentScope();

        auditEvent.ActorUserId =
            user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        auditEvent.ActorUserName =
            user?.Identity?.Name ?? "unknown";

        auditEvent.TenantId = scope.TenantId;
        auditEvent.WorkspaceId = scope.WorkspaceId;
        auditEvent.ProjectId = scope.ProjectId;

        if (string.IsNullOrWhiteSpace(auditEvent.DataJson))
            auditEvent.DataJson = "{}";

        if (string.IsNullOrWhiteSpace(auditEvent.CorrelationId))
        {
            auditEvent.CorrelationId =
                Activity.Current?.Id
                ?? http?.TraceIdentifier;
        }

        await _repo.AppendAsync(auditEvent, ct);
    }
}
