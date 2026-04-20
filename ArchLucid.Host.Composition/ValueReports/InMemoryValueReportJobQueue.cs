using System.Collections.Concurrent;
using System.Text.Json;

using ArchLucid.Application.Value;
using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.Contracts.ValueReports;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Composition.ValueReports;

/// <summary>
/// In-process async generation for large windows (see <c>ValueReportComputationOptions.AsyncJobWhenWindowDaysExceeds</c>).
/// </summary>
public sealed class InMemoryValueReportJobQueue(
    IServiceScopeFactory scopeFactory,
    ILogger<InMemoryValueReportJobQueue> logger) : IValueReportJobQueue
{
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<InMemoryValueReportJobQueue> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ConcurrentDictionary<Guid, JobEntry> _jobs = new();

    public Guid Enqueue(ValueReportJobRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        Guid jobId = Guid.NewGuid();
        JobEntry entry = new(request, JobPhase.Pending, null, null, null);

        if (!_jobs.TryAdd(jobId, entry))
            throw new InvalidOperationException("Duplicate job id (extremely unlikely).");

        _ = RunJobAsync(jobId, request);

        return jobId;
    }

    public ValueReportJobPollResult TryPoll(Guid jobId, Guid scopedTenantId)
    {
        if (!_jobs.TryGetValue(jobId, out JobEntry? entry))
            return new ValueReportJobPollResult(false, false, null, null, null);

        if (entry.Request.TenantId != scopedTenantId)
            return new ValueReportJobPollResult(false, false, null, null, null);

        return entry.Phase switch
        {
            JobPhase.Pending => new ValueReportJobPollResult(true, false, null, entry.FileName, null),
            JobPhase.Completed => new ValueReportJobPollResult(true, true, entry.Bytes, entry.FileName, null),
            JobPhase.Failed => new ValueReportJobPollResult(true, false, null, entry.FileName, entry.ErrorMessage),
            _ => new ValueReportJobPollResult(true, false, null, null, "Unknown job phase."),
        };
    }

    private async Task RunJobAsync(Guid jobId, ValueReportJobRequest request)
    {
        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

            using (AmbientScopeContext.Push(
                       new ScopeContext
                       {
                           TenantId = request.TenantId,
                           WorkspaceId = request.WorkspaceId,
                           ProjectId = request.ProjectId,
                       }))
            {
                ValueReportBuilder builder = scope.ServiceProvider.GetRequiredService<ValueReportBuilder>();
                IValueReportRenderer renderer = scope.ServiceProvider.GetRequiredService<IValueReportRenderer>();
                IAuditService audit = scope.ServiceProvider.GetRequiredService<IAuditService>();

                ValueReportSnapshot snapshot = await builder.BuildAsync(
                    request.TenantId,
                    request.WorkspaceId,
                    request.ProjectId,
                    request.FromUtcInclusive,
                    request.ToUtcExclusive,
                    CancellationToken.None);

                byte[] docx = await renderer.RenderAsync(snapshot, CancellationToken.None);

                string fileName =
                    $"ArchLucid-value-report-{request.TenantId:N}-{request.FromUtcInclusive:yyyyMMdd}-{request.ToUtcExclusive:yyyyMMdd}.docx";

                await audit.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.ValueReportGenerated,
                        DataJson = JsonSerializer.Serialize(
                            new
                            {
                                jobId,
                                byteCount = docx.Length,
                                asyncJob = true,
                            }),
                    },
                    CancellationToken.None);

                _jobs[jobId] = new JobEntry(request, JobPhase.Completed, docx, fileName, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Value report job {JobId} failed.", jobId);
            _jobs.TryGetValue(jobId, out JobEntry? existing);
            ValueReportJobRequest req = existing?.Request ?? request;
            _jobs[jobId] = new JobEntry(req, JobPhase.Failed, null, null, ex.Message);
        }
    }

    private sealed record JobEntry(
        ValueReportJobRequest Request,
        JobPhase Phase,
        byte[]? Bytes,
        string? FileName,
        string? ErrorMessage);

    private enum JobPhase
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
    }
}
