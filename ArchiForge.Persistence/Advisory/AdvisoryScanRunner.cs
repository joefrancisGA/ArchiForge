using System.Text.Json;

using ArchiForge.Core.Audit;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Queries;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// Executes a scheduled advisory scan: compares runs, builds an improvement plan, merges effective policy defaults, evaluates alerts, and delivers a digest.
/// </summary>
/// <param name="authorityQueryService">Loads latest runs and golden manifests for the project slug.</param>
/// <param name="improvementAdvisorService">Generates <see cref="ImprovementPlan"/> from findings.</param>
/// <param name="comparisonService">Optional run-to-run comparison when a previous run exists.</param>
/// <param name="digestBuilder">Builds the architecture digest payload from plan + alerts.</param>
/// <param name="digestRepository">Persists digest rows.</param>
/// <param name="deliveryDispatcher">Sends digest to configured channels.</param>
/// <param name="alertService">Simple alert evaluation for the scan context.</param>
/// <param name="compositeAlertService">Composite alert evaluation for the same context.</param>
/// <param name="effectiveGovernanceLoader">Supplies merged policy content for advisory defaults and alert/compliance filtering.</param>
/// <param name="recommendationRepository">Historical recommendations for the run.</param>
/// <param name="recommendationLearningService">Learning profile for advisory context.</param>
/// <param name="executionRepository">Tracks scan execution lifecycle.</param>
/// <param name="scheduleRepository">Schedule metadata (advance after success/failure).</param>
/// <param name="scheduleCalculator">Next-run scheduling.</param>
/// <param name="auditService">Audit events for scan, digest, and related actions.</param>
/// <remarks>
/// Pushes <see cref="AmbientScopeContext"/> for the schedule’s tenant/workspace/project so downstream providers (compliance, governance) resolve the correct scope.
/// Loads <see cref="IEffectiveGovernanceLoader.LoadEffectiveContentAsync"/> once per successful scan and passes it into <see cref="AlertEvaluationContextFactory.ForAdvisoryScan"/> so alert services avoid a second governance load.
/// </remarks>
public sealed class AdvisoryScanRunner(
    IAuthorityQueryService authorityQueryService,
    IImprovementAdvisorService improvementAdvisorService,
    IComparisonService comparisonService,
    IArchitectureDigestBuilder digestBuilder,
    IArchitectureDigestRepository digestRepository,
    IDigestDeliveryDispatcher deliveryDispatcher,
    IAlertService alertService,
    ICompositeAlertService compositeAlertService,
    IEffectiveGovernanceLoader effectiveGovernanceLoader,
    IRecommendationRepository recommendationRepository,
    IRecommendationLearningService recommendationLearningService,
    IAdvisoryScanExecutionRepository executionRepository,
    IAdvisoryScanScheduleRepository scheduleRepository,
    IScanScheduleCalculator scheduleCalculator,
    IAuditService auditService) : IAdvisoryScanRunner
{
    /// <summary>
    /// Creates an execution record, runs the scan under ambient scope, and advances the schedule; failures are recorded and the schedule still advances.
    /// </summary>
    /// <param name="schedule">Tenant/workspace/project and cadence metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task RunScheduleAsync(AdvisoryScanSchedule schedule, CancellationToken ct)
    {
        var scope = new ScopeContext
        {
            TenantId = schedule.TenantId,
            WorkspaceId = schedule.WorkspaceId,
            ProjectId = schedule.ProjectId
        };

        var execution = new AdvisoryScanExecution
        {
            ExecutionId = Guid.NewGuid(),
            ScheduleId = schedule.ScheduleId,
            TenantId = schedule.TenantId,
            WorkspaceId = schedule.WorkspaceId,
            ProjectId = schedule.ProjectId,
            StartedUtc = DateTime.UtcNow,
            Status = "Started",
            ResultJson = "{}"
        };

        await executionRepository.CreateAsync(execution, ct).ConfigureAwait(false);

        try
        {
            using (AmbientScopeContext.Push(scope))
            {
                await RunScheduleCoreAsync(schedule, scope, execution, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            execution.Status = "Failed";
            execution.CompletedUtc = DateTime.UtcNow;
            execution.ErrorMessage = ex.Message;
            await executionRepository.UpdateAsync(execution, ct).ConfigureAwait(false);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.AdvisoryScanExecuted,
                    DataJson = JsonSerializer.Serialize(new
                    {
                        scheduleId = schedule.ScheduleId,
                        executionId = execution.ExecutionId,
                        failed = true,
                        error = ex.Message
                    }),
                },
                ct).ConfigureAwait(false);

            await AdvanceScheduleAsync(schedule, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Core scan path after ambient scope is pushed: plan generation, governance merge into the plan, alert evaluation, digest persistence and delivery.
    /// </summary>
    /// <param name="schedule">Active schedule row.</param>
    /// <param name="scope">Same ids as the schedule; used for queries.</param>
    /// <param name="execution">Execution row updated to completed/failed states by this method and helpers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Copies merged <see cref="PolicyPackContentDocument.AdvisoryDefaults"/> into <see cref="ImprovementPlan.PolicyPackAdvisoryDefaults"/> before building <see cref="AlertEvaluationContext"/>.
    /// </remarks>
    private async Task RunScheduleCoreAsync(
        AdvisoryScanSchedule schedule,
        ScopeContext scope,
        AdvisoryScanExecution execution,
        CancellationToken ct)
    {
        var slug = string.IsNullOrWhiteSpace(schedule.RunProjectSlug) ? "default" : schedule.RunProjectSlug.Trim();
        var runs = await authorityQueryService
            .ListRunsByProjectAsync(scope, slug, 2, ct)
            .ConfigureAwait(false);

        var ordered = runs.OrderByDescending(x => x.CreatedUtc).ToList();
        var latest = ordered.FirstOrDefault();
        if (latest is null)
        {
            await CompleteNoRunsAsync(execution, schedule, ct).ConfigureAwait(false);
            return;
        }

        var latestDetail = await authorityQueryService
            .GetRunDetailAsync(scope, latest.RunId, ct)
            .ConfigureAwait(false);

        if (latestDetail?.GoldenManifest is null)
        {
            await FailAsync(
                execution,
                schedule,
                "Latest run did not contain a golden manifest.",
                ct).ConfigureAwait(false);
            return;
        }

        var findings = latestDetail.FindingsSnapshot ?? CreateEmptyFindings(latestDetail.GoldenManifest);
        var compareTo = ordered.Skip(1).FirstOrDefault();

        ImprovementPlan plan;
        Guid? comparedToRunId = null;
        ComparisonResult? comparisonResult = null;

        if (compareTo is not null)
        {
            var previousDetail = await authorityQueryService
                .GetRunDetailAsync(scope, compareTo.RunId, ct)
                .ConfigureAwait(false);

            if (previousDetail?.GoldenManifest is not null)
            {
                comparisonResult = comparisonService.Compare(previousDetail.GoldenManifest, latestDetail.GoldenManifest);
                comparedToRunId = compareTo.RunId;
                plan = await improvementAdvisorService
                    .GeneratePlanAsync(latestDetail.GoldenManifest, findings, comparisonResult, ct)
                    .ConfigureAwait(false);
            }
            else
            {
                plan = await improvementAdvisorService
                    .GeneratePlanAsync(latestDetail.GoldenManifest, findings, ct)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            plan = await improvementAdvisorService
                .GeneratePlanAsync(latestDetail.GoldenManifest, findings, ct)
                .ConfigureAwait(false);
        }

        var recommendationRecords = await recommendationRepository
            .ListByRunAsync(schedule.TenantId, schedule.WorkspaceId, schedule.ProjectId, latest.RunId, ct)
            .ConfigureAwait(false);

        var learningProfile = await recommendationLearningService
            .GetLatestProfileAsync(schedule.TenantId, schedule.WorkspaceId, schedule.ProjectId, ct)
            .ConfigureAwait(false);

        var effectiveGovernance = await effectiveGovernanceLoader
            .LoadEffectiveContentAsync(schedule.TenantId, schedule.WorkspaceId, schedule.ProjectId, ct)
            .ConfigureAwait(false);

        foreach (var kvp in effectiveGovernance.AdvisoryDefaults)
            plan.PolicyPackAdvisoryDefaults[kvp.Key] = kvp.Value;

        var alertContext = AlertEvaluationContextFactory.ForAdvisoryScan(
            schedule.TenantId,
            schedule.WorkspaceId,
            schedule.ProjectId,
            latest.RunId,
            comparedToRunId,
            plan,
            comparisonResult,
            recommendationRecords,
            learningProfile,
            effectiveGovernance);

        var alertOutcome = await alertService.EvaluateAndPersistAsync(alertContext, ct).ConfigureAwait(false);

        var compositeOutcome = await compositeAlertService
            .EvaluateAndPersistAsync(alertContext, ct)
            .ConfigureAwait(false);

        var digestAlerts = alertOutcome.Evaluated
            .Concat(compositeOutcome.Created)
            .ToList();

        var digest = digestBuilder.Build(
            schedule.TenantId,
            schedule.WorkspaceId,
            schedule.ProjectId,
            latest.RunId,
            comparedToRunId,
            plan,
            digestAlerts);

        await digestRepository.CreateAsync(digest, ct).ConfigureAwait(false);

        await deliveryDispatcher.DeliverAsync(digest, ct).ConfigureAwait(false);

        execution.Status = "Completed";
        execution.CompletedUtc = DateTime.UtcNow;
        execution.ResultJson = JsonSerializer.Serialize(new
        {
            runId = latest.RunId,
            comparedToRunId,
            recommendationCount = plan.Recommendations.Count,
            digestId = digest.DigestId,
            alertsEvaluated = alertOutcome.Evaluated.Count,
            alertsNewlyPersisted = alertOutcome.NewlyPersisted.Count,
            compositeAlertsCreated = compositeOutcome.Created.Count,
            compositeAlertsSuppressed = compositeOutcome.SuppressedMatchCount,
        });

        await executionRepository.UpdateAsync(execution, ct).ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AdvisoryScanExecuted,
                RunId = latest.RunId,
                DataJson = JsonSerializer.Serialize(new { scheduleId = schedule.ScheduleId, executionId = execution.ExecutionId }),
            },
            ct).ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ArchitectureDigestGenerated,
                RunId = latest.RunId,
                DataJson = JsonSerializer.Serialize(new { digestId = digest.DigestId, scheduleId = schedule.ScheduleId }),
            },
            ct).ConfigureAwait(false);

        await AdvanceScheduleAsync(schedule, ct).ConfigureAwait(false);
    }

    private async Task CompleteNoRunsAsync(
        AdvisoryScanExecution execution,
        AdvisoryScanSchedule schedule,
        CancellationToken ct)
    {
        execution.Status = "Completed";
        execution.CompletedUtc = DateTime.UtcNow;
        execution.ResultJson = """{"message":"No runs were available."}""";
        await executionRepository.UpdateAsync(execution, ct).ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AdvisoryScanExecuted,
                DataJson = JsonSerializer.Serialize(new { scheduleId = schedule.ScheduleId, message = "no_runs" }),
            },
            ct).ConfigureAwait(false);

        await AdvanceScheduleAsync(schedule, ct).ConfigureAwait(false);
    }

    private async Task FailAsync(
        AdvisoryScanExecution execution,
        AdvisoryScanSchedule schedule,
        string message,
        CancellationToken ct)
    {
        execution.Status = "Failed";
        execution.CompletedUtc = DateTime.UtcNow;
        execution.ErrorMessage = message;
        await executionRepository.UpdateAsync(execution, ct).ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AdvisoryScanExecuted,
                DataJson = JsonSerializer.Serialize(new { scheduleId = schedule.ScheduleId, failed = true, message }),
            },
            ct).ConfigureAwait(false);

        await AdvanceScheduleAsync(schedule, ct).ConfigureAwait(false);
    }

    private async Task AdvanceScheduleAsync(AdvisoryScanSchedule schedule, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        schedule.LastRunUtc = now;
        schedule.NextRunUtc = scheduleCalculator.ComputeNextRunUtc(schedule.CronExpression, now);
        await scheduleRepository.UpdateAsync(schedule, ct).ConfigureAwait(false);
    }

    private static FindingsSnapshot CreateEmptyFindings(GoldenManifest manifest) =>
        new()
        {
            SchemaVersion = FindingsSchema.CurrentSnapshotVersion,
            FindingsSnapshotId = manifest.FindingsSnapshotId,
            RunId = manifest.RunId,
            ContextSnapshotId = manifest.ContextSnapshotId,
            GraphSnapshotId = manifest.GraphSnapshotId,
            CreatedUtc = manifest.CreatedUtc,
            Findings = []
        };
}
