using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

/// <summary>
///     Builds a deterministic 59R planning report from the planning repository (stable ordering, capped evidence
///     lists).
/// </summary>
public static class LearningPlanningReportBuilder
{
    public static async Task<LearningPlanningReportDocument> BuildAsync(
        IProductLearningPlanningRepository repository,
        ProductLearningScope scope,
        LearningPlanningReportLimits limits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(limits);

        IReadOnlyList<ProductLearningImprovementThemeRecord> themeRows =
            await repository.ListThemesAsync(scope, limits.MaxThemes, cancellationToken);

        IReadOnlyList<ProductLearningImprovementPlanRecord> planRows =
            await repository.ListPlansAsync(scope, limits.MaxPlans, cancellationToken);

        Dictionary<Guid, string> themeTitleById = themeRows.ToDictionary(t => t.ThemeId, t => t.Title);

        List<ProductLearningImprovementThemeRecord> sortedThemes = themeRows
            .OrderByDescending(t => t.EvidenceSignalCount)
            .ThenByDescending(t => t.DistinctRunCount)
            .ThenBy(t => t.ThemeId)
            .ToList();

        List<ProductLearningImprovementPlanRecord> sortedPlans = planRows
            .OrderByDescending(p => p.PriorityScore)
            .ThenBy(p => p.PlanId)
            .ToList();

        PlanHydration[] hydrated = await Task.WhenAll(
                sortedPlans.Select(p => HydratePlanAsync(repository, scope, p, cancellationToken)))
            .ConfigureAwait(false);

        int totalLinkedSignals = hydrated.Sum(h => h.SignalRows.Count);

        List<LearningPlanningReportPlanEntry> planEntries = [];

        for (int i = 0; i < sortedPlans.Count; i++)
        {
            ProductLearningImprovementPlanRecord p = sortedPlans[i];
            PlanHydration h = hydrated[i];

            List<LearningPlanningReportSignalRef> signalRefs = h.SignalRows
                .Take(limits.MaxSignalRefsPerPlan)
                .Select(s => new LearningPlanningReportSignalRef
                {
                    SignalId = s.SignalId, TriageStatusSnapshot = s.TriageStatusSnapshot
                })
                .ToList();

            List<LearningPlanningReportArtifactRef> artifactRefs = h.ArtifactRows
                .Take(limits.MaxArtifactRefsPerPlan)
                .Select(a => new LearningPlanningReportArtifactRef
                {
                    LinkId = a.LinkId,
                    AuthorityBundleId = a.AuthorityBundleId,
                    AuthorityArtifactSortOrder = a.AuthorityArtifactSortOrder,
                    PilotArtifactHint = a.PilotArtifactHint
                })
                .ToList();

            List<string> runRefs = h.RunRows.Take(limits.MaxRunRefsPerPlan).ToList();

            string themeTitle = themeTitleById.TryGetValue(p.ThemeId, out string? tt) ? tt : p.ThemeId.ToString("D");

            planEntries.Add(
                new LearningPlanningReportPlanEntry
                {
                    PlanId = p.PlanId,
                    ThemeId = p.ThemeId,
                    ThemeTitle = themeTitle,
                    Title = p.Title,
                    Summary = p.Summary,
                    PriorityScore = p.PriorityScore,
                    PriorityExplanation = p.PriorityExplanation,
                    Status = p.Status,
                    CreatedUtc = p.CreatedUtc,
                    ActionStepCount = p.ActionSteps.Count,
                    Evidence = new LearningPlanningReportPlanEvidenceBlock
                    {
                        LinkedSignalCount = h.SignalRows.Count,
                        LinkedArtifactCount = h.ArtifactRows.Count,
                        LinkedArchitectureRunCount = h.RunRows.Count,
                        Signals = signalRefs,
                        Artifacts = artifactRefs,
                        ArchitectureRunIds = runRefs
                    }
                });
        }

        List<LearningPlanningReportThemeEntry> themeEntries = sortedThemes
            .Select(t => new LearningPlanningReportThemeEntry
            {
                ThemeId = t.ThemeId,
                ThemeKey = t.ThemeKey,
                Title = t.Title,
                Summary = t.Summary,
                SeverityBand = t.SeverityBand,
                EvidenceSignalCount = t.EvidenceSignalCount,
                DistinctRunCount = t.DistinctRunCount,
                Status = t.Status
            })
            .ToList();

        LearningPlanningReportSummaryBlock summary = new()
        {
            ThemeCount = themeRows.Count,
            PlanCount = planRows.Count,
            TotalThemeEvidenceSignals = themeRows.Sum(t => t.EvidenceSignalCount),
            TotalLinkedSignalsAcrossPlans = totalLinkedSignals,
            MaxPlanPriorityScore = planRows.Count == 0 ? null : planRows.Max(p => p.PriorityScore)
        };

        return new LearningPlanningReportDocument
        {
            GeneratedUtc = DateTime.UtcNow, Summary = summary, Themes = themeEntries, Plans = planEntries
        };
    }

    private static async Task<PlanHydration> HydratePlanAsync(
        IProductLearningPlanningRepository repository,
        ProductLearningScope scope,
        ProductLearningImprovementPlanRecord plan,
        CancellationToken cancellationToken)
    {
        Task<IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord>> signalsTask =
            repository.ListPlanSignalLinksAsync(plan.PlanId, scope, cancellationToken);

        Task<IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord>> artifactsTask =
            repository.ListPlanArtifactLinksAsync(plan.PlanId, scope, cancellationToken);

        Task<IReadOnlyList<string>> runsTask =
            repository.ListPlanArchitectureRunIdsAsync(plan.PlanId, scope, cancellationToken);

        await Task.WhenAll(signalsTask, artifactsTask, runsTask).ConfigureAwait(false);

        IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord> signalRows =
            await signalsTask.ConfigureAwait(false);
        IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord> artifactRows =
            await artifactsTask.ConfigureAwait(false);
        IReadOnlyList<string> runRows = await runsTask.ConfigureAwait(false);

        return new PlanHydration(signalRows, artifactRows, runRows);
    }

    private readonly record struct PlanHydration(
        IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord> SignalRows,
        IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord> ArtifactRows,
        IReadOnlyList<string> RunRows);
}
