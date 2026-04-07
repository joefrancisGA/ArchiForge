using ArchLucid.Api.Models.Learning;
using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

namespace ArchLucid.Api.Services;

public sealed class LearningPlanningReadService(IProductLearningPlanningRepository planningRepository)
    : ILearningPlanningReadService
{
    public async Task<LearningThemesListResponse> GetThemesAsync(
        ProductLearningScope scope,
        int maxThemes,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductLearningImprovementThemeRecord> rows =
            await planningRepository.ListThemesAsync(scope, maxThemes, cancellationToken);

        DateTime generatedUtc = DateTime.UtcNow;
        List<LearningThemeResponse> themes = rows.Select(MapTheme).ToList();

        return new LearningThemesListResponse
        {
            GeneratedUtc = generatedUtc,
            Themes = themes,
        };
    }

    public async Task<LearningPlansListResponse> GetPlansAsync(
        ProductLearningScope scope,
        int maxPlans,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductLearningImprovementPlanRecord> rows =
            await planningRepository.ListPlansAsync(scope, maxPlans, cancellationToken);

        DateTime generatedUtc = DateTime.UtcNow;

        Guid[] distinctThemeIds = rows.Select(r => r.ThemeId).Distinct().ToArray();

        ProductLearningImprovementThemeRecord?[] themeRows =
            await Task.WhenAll(
                    distinctThemeIds.Select(id =>
                        planningRepository.GetThemeAsync(id, scope, cancellationToken)))
                .ConfigureAwait(false);

        Dictionary<Guid, ProductLearningImprovementThemeRecord?> themeById = [];
        for (int i = 0; i < distinctThemeIds.Length; i++)
        {
            themeById[distinctThemeIds[i]] = themeRows[i];
        }

        List<LearningPlanListItemResponse> plans = rows
            .Select(p => MapPlanListItem(p, themeById.TryGetValue(p.ThemeId, out ProductLearningImprovementThemeRecord? t) ? t : null))
            .ToList();

        return new LearningPlansListResponse
        {
            GeneratedUtc = generatedUtc,
            Plans = plans,
        };
    }

    public async Task<LearningPlanDetailResponse?> GetPlanByIdAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningImprovementPlanRecord? plan =
            await planningRepository.GetPlanAsync(planId, scope, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        ProductLearningImprovementThemeRecord? theme =
            await planningRepository.GetThemeAsync(plan.ThemeId, scope, cancellationToken);

        Task<IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord>> signalsTask =
            planningRepository.ListPlanSignalLinksAsync(planId, scope, cancellationToken);

        Task<IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord>> artifactsTask =
            planningRepository.ListPlanArtifactLinksAsync(planId, scope, cancellationToken);

        Task<IReadOnlyList<string>> runsTask =
            planningRepository.ListPlanArchitectureRunIdsAsync(planId, scope, cancellationToken);

        await Task.WhenAll(signalsTask, artifactsTask, runsTask).ConfigureAwait(false);

        IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord> signals = await signalsTask.ConfigureAwait(false);
        IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord> artifacts = await artifactsTask.ConfigureAwait(false);
        IReadOnlyList<string> runs = await runsTask.ConfigureAwait(false);

        return new LearningPlanDetailResponse
        {
            PlanId = plan.PlanId,
            ThemeId = plan.ThemeId,
            Title = plan.Title,
            Summary = plan.Summary,
            PriorityScore = plan.PriorityScore,
            PriorityExplanation = plan.PriorityExplanation,
            Status = plan.Status,
            CreatedUtc = plan.CreatedUtc,
            CreatedByUserId = plan.CreatedByUserId,
            ActionSteps = plan.ActionSteps.Select(MapStep).ToList(),
            EvidenceCounts = new LearningPlanEvidenceCountsResponse
            {
                LinkedSignalCount = signals.Count,
                LinkedArtifactCount = artifacts.Count,
                LinkedArchitectureRunCount = runs.Count,
            },
            Theme = theme is null ? null : MapTheme(theme),
        };
    }

    public async Task<LearningSummaryResponse> GetSummaryAsync(
        ProductLearningScope scope,
        int maxThemes,
        int maxPlans,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductLearningImprovementThemeRecord> themes =
            await planningRepository.ListThemesAsync(scope, maxThemes, cancellationToken);

        IReadOnlyList<ProductLearningImprovementPlanRecord> plans =
            await planningRepository.ListPlansAsync(scope, maxPlans, cancellationToken);

        int[] linkCounts = await Task.WhenAll(
                plans.Select(async p =>
                    (await planningRepository
                        .ListPlanSignalLinksAsync(p.PlanId, scope, cancellationToken)
                        .ConfigureAwait(false))
                    .Count))
            .ConfigureAwait(false);

        DateTime generatedUtc = DateTime.UtcNow;
        int totalThemeEvidence = themes.Sum(t => t.EvidenceSignalCount);

        return new LearningSummaryResponse
        {
            GeneratedUtc = generatedUtc,
            ThemeCount = themes.Count,
            PlanCount = plans.Count,
            TotalThemeEvidenceSignals = totalThemeEvidence,
            MaxPlanPriorityScore = plans.Count == 0 ? null : plans.Max(p => p.PriorityScore),
            TotalLinkedSignalsAcrossPlans = linkCounts.Sum(),
        };
    }

    public Task<LearningPlanningReportDocument> GetPlanningReportAsync(
        ProductLearningScope scope,
        LearningPlanningReportLimits limits,
        CancellationToken cancellationToken) =>
        LearningPlanningReportBuilder.BuildAsync(planningRepository, scope, limits, cancellationToken);

    private static LearningThemeResponse MapTheme(ProductLearningImprovementThemeRecord t) =>
        new()
        {
            ThemeId = t.ThemeId,
            ThemeKey = t.ThemeKey,
            SourceAggregateKey = t.SourceAggregateKey,
            PatternKey = t.PatternKey,
            Title = t.Title,
            Summary = t.Summary,
            AffectedArtifactTypeOrWorkflowArea = t.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = t.SeverityBand,
            EvidenceSignalCount = t.EvidenceSignalCount,
            DistinctRunCount = t.DistinctRunCount,
            AverageTrustScore = t.AverageTrustScore,
            DerivationRuleVersion = t.DerivationRuleVersion,
            Status = t.Status,
            CreatedUtc = t.CreatedUtc,
            CreatedByUserId = t.CreatedByUserId,
        };

    private static LearningPlanListItemResponse MapPlanListItem(
        ProductLearningImprovementPlanRecord p,
        ProductLearningImprovementThemeRecord? theme) =>
        new()
        {
            PlanId = p.PlanId,
            ThemeId = p.ThemeId,
            Title = p.Title,
            Summary = p.Summary,
            PriorityScore = p.PriorityScore,
            PriorityExplanation = p.PriorityExplanation,
            Status = p.Status,
            CreatedUtc = p.CreatedUtc,
            ThemeEvidenceSignalCount = theme?.EvidenceSignalCount,
        };

    private static LearningPlanStepResponse MapStep(ProductLearningImprovementPlanActionStep s) =>
        new()
        {
            Ordinal = s.Ordinal,
            ActionType = s.ActionType,
            Description = s.Description,
            AcceptanceCriteria = s.AcceptanceCriteria,
        };
}
