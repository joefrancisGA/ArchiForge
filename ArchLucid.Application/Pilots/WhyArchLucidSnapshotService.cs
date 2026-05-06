using ArchLucid.Application.Bootstrap;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;
/// <summary>
///     Default <see cref = "IWhyArchLucidSnapshotService"/> that combines cumulative
///     <see cref = "ArchLucidInstrumentation"/> counters (via an
///     <see cref = "IInstrumentationCounterSnapshotProvider"/>) with a default-scope audit row count and the
///     canonical Contoso Retail demo run id.
/// </summary>
public sealed class WhyArchLucidSnapshotService(IInstrumentationCounterSnapshotProvider counters, IAuditRepository auditRepository, TimeProvider timeProvider, ILogger<WhyArchLucidSnapshotService> logger) : IWhyArchLucidSnapshotService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(counters, auditRepository, timeProvider, logger);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Core.Diagnostics.IInstrumentationCounterSnapshotProvider counters, ArchLucid.Persistence.Audit.IAuditRepository auditRepository, System.TimeProvider timeProvider, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.Pilots.WhyArchLucidSnapshotService> logger)
    {
        ArgumentNullException.ThrowIfNull(counters);
        ArgumentNullException.ThrowIfNull(auditRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    /// <inheritdoc/>
    public async Task<WhyArchLucidSnapshotResponse> BuildAsync(CancellationToken cancellationToken)
    {
        if (counters is null)
            throw new ArgumentNullException(nameof(counters));
        if (auditRepository is null)
            throw new ArgumentNullException(nameof(auditRepository));
        if (timeProvider is null)
            throw new ArgumentNullException(nameof(timeProvider));
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));
        InstrumentationCounterSnapshot snapshot = counters.GetSnapshot();
        int auditCount = 0;
        bool truncated = false;
        try
        {
            IReadOnlyList<AuditEvent> events = await auditRepository.GetByScopeAsync(ScopeIds.DefaultTenant, ScopeIds.DefaultWorkspace, ScopeIds.DefaultProject, WhyArchLucidSnapshotResponse.AuditRowCountCap, cancellationToken);
            auditCount = events.Count;
            truncated = events.Count >= WhyArchLucidSnapshotResponse.AuditRowCountCap;
        }
        catch (Exception ex)when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Why-ArchLucid snapshot: audit row count unavailable; reporting 0.");
        }

        double hours = PilotHoursSavedEstimator.Estimate(snapshot.RunsCreatedTotal, snapshot.FindingsProducedBySeverity, auditCount);
        return new WhyArchLucidSnapshotResponse
        {
            GeneratedUtc = timeProvider.GetUtcNow(),
            DemoRunId = ContosoRetailDemoIdentifiers.RunBaseline,
            RunsCreatedTotal = snapshot.RunsCreatedTotal,
            FindingsProducedBySeverity = snapshot.FindingsProducedBySeverity,
            AuditRowCount = auditCount,
            AuditRowCountTruncated = truncated,
            EstimatedManualWorkHoursSaved = hours,
            EstimatedManualWorkHoursSavedMethodology = PilotHoursSavedEstimator.Methodology
        };
    }
}