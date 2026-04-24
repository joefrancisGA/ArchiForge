using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Audit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     API factory for the governance dry-run integration tests. Replaces <see cref="IAuditRepository" /> with
///     <see cref="CapturingAuditRepository" /> so the test can assert the persisted <c>DataJson</c> contains
///     the LLM-prompt redaction marker (PENDING_QUESTIONS Q37).
/// </summary>
public sealed class PolicyPackDryRunIntegrationApiFactory : ArchLucidApiFactory
{
    public CapturingAuditRepository Audit
    {
        get;
    } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuditRepository>();
            services.AddSingleton<IAuditRepository>(Audit);
        });
    }
}

/// <summary>
///     Minimal in-memory audit repository that retains every appended <see cref="AuditEvent" /> verbatim so
///     tests can assert payload contents (including the redaction marker) without going through SQL.
/// </summary>
public sealed class CapturingAuditRepository : IAuditRepository
{
    private readonly List<AuditEvent> _events = [];
    private readonly Lock _gate = new();

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        lock (_gate) _events.Add(auditEvent);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEvent>> GetByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    }

    public Task<IReadOnlyList<AuditEvent>> GetFilteredAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        AuditEventFilter filter,
        CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    }

    public Task<IReadOnlyList<AuditEvent>> GetExportAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxRows,
        CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    }

    public IReadOnlyList<AuditEvent> Snapshot()
    {
        lock (_gate) return [.._events];
    }
}
