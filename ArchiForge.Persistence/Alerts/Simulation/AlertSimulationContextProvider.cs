using ArchiForge.Core.Comparison;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Simulation;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Queries;

namespace ArchiForge.Persistence.Alerts.Simulation;

/// <summary>
/// Replays advisory-style plan generation for historical runs to produce <see cref="AlertEvaluationContext"/> for simulation APIs or tooling.
/// </summary>
/// <param name="authorityQueryService">Loads run detail and golden manifests.</param>
/// <param name="improvementAdvisorService">Builds <see cref="ImprovementPlan"/> from manifest and findings.</param>
/// <param name="comparisonService">Optional baseline-vs-latest comparison.</param>
/// <param name="recommendationRepository">Recommendations per run.</param>
/// <param name="recommendationLearningService">Learning profile for the scope.</param>
/// <remarks>
/// Does not set <see cref="AlertEvaluationContext.EffectiveGovernanceContent"/>; downstream evaluation loads merged policy when needed.
/// </remarks>
public sealed class AlertSimulationContextProvider(
    IAuthorityQueryService authorityQueryService,
    IImprovementAdvisorService improvementAdvisorService,
    IComparisonService comparisonService,
    IRecommendationRepository recommendationRepository,
    IRecommendationLearningService recommendationLearningService) : IAlertSimulationContextProvider
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertEvaluationContext>> GetContextsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? comparedToRunId,
        int recentRunCount,
        string runProjectSlug,
        CancellationToken ct)
    {
        var scope = new ScopeContext
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
        };

        var results = new List<AlertEvaluationContext>();

        if (runId.HasValue)
        {
            var single = await BuildContextAsync(
                    scope,
                    runId.Value,
                    comparedToRunId,
                    ct)
                .ConfigureAwait(false);

            if (single is not null)
                results.Add(single);

            return results;
        }

        var take = Math.Clamp(recentRunCount, 1, 50);
        var runs = await authorityQueryService
            .ListRunsByProjectAsync(scope, string.IsNullOrWhiteSpace(runProjectSlug) ? "default" : runProjectSlug.Trim(), take, ct)
            .ConfigureAwait(false);

        foreach (var run in runs.OrderByDescending(x => x.CreatedUtc))
        {
            var context = await BuildContextAsync(scope, run.RunId, comparedToRunId: null, ct).ConfigureAwait(false);
            if (context is not null)
                results.Add(context);
        }

        return results;
    }

    /// <summary>
    /// Loads run detail, builds plan (with optional comparison), attaches recommendations and learning profile.
    /// </summary>
    /// <returns><c>null</c> when the run has no golden manifest.</returns>
    private async Task<AlertEvaluationContext?> BuildContextAsync(
        ScopeContext scope,
        Guid runId,
        Guid? comparedToRunId,
        CancellationToken ct)
    {
        var detail = await authorityQueryService.GetRunDetailAsync(scope, runId, ct).ConfigureAwait(false);
        if (detail?.GoldenManifest is null)
            return null;

        var findings = detail.FindingsSnapshot ?? CreateEmptyFindings(detail.GoldenManifest);

        ComparisonResult? comparison = null;

        if (comparedToRunId.HasValue)
        {
            var comparedDetail = await authorityQueryService
                .GetRunDetailAsync(scope, comparedToRunId.Value, ct)
                .ConfigureAwait(false);

            if (comparedDetail?.GoldenManifest is not null)
                comparison = comparisonService.Compare(comparedDetail.GoldenManifest, detail.GoldenManifest);
        }

        var plan = comparison is null
            ? await improvementAdvisorService
                .GeneratePlanAsync(detail.GoldenManifest, findings, ct)
                .ConfigureAwait(false)
            : await improvementAdvisorService
                .GeneratePlanAsync(detail.GoldenManifest, findings, comparison, ct)
                .ConfigureAwait(false);

        var recommendations = await recommendationRepository
            .ListByRunAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, runId, ct)
            .ConfigureAwait(false);

        var learning = await recommendationLearningService
            .GetLatestProfileAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, ct)
            .ConfigureAwait(false);

        return new AlertEvaluationContext
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            RunId = runId,
            ComparedToRunId = comparedToRunId,
            ImprovementPlan = plan,
            ComparisonResult = comparison,
            RecommendationRecords = recommendations,
            LearningProfile = learning,
        };
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
