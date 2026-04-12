using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

/// <summary>
/// In-memory 59R planning store for development/tests.
/// Does not validate <c>dbo.Runs</c>, pilot signals, or authority artifacts (SQL repository does).
/// </summary>
public sealed class InMemoryProductLearningPlanningRepository : IProductLearningPlanningRepository
{
    private readonly List<ProductLearningImprovementThemeRecord> _themes = [];

    private readonly List<ProductLearningImprovementPlanRecord> _plans = [];

    private readonly List<ProductLearningImprovementPlanRunLinkRecord> _runLinks = [];

    private readonly List<ProductLearningImprovementPlanSignalLinkRecord> _signalLinks = [];

    private readonly List<ProductLearningImprovementPlanArtifactLinkRecord> _artifactLinks = [];

    public Task InsertThemeAsync(ProductLearningImprovementThemeRecord theme, CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        string status = ProductLearningPlanningRepositoryValidation.NormalizeThemeStatus(theme.Status);
        Guid themeId = theme.ThemeId == Guid.Empty ? Guid.NewGuid() : theme.ThemeId;
        DateTime createdUtc = theme.CreatedUtc == default ? DateTime.UtcNow : theme.CreatedUtc;

        if (_themes.Any(t =>
                t.TenantId == theme.TenantId &&
                t.WorkspaceId == theme.WorkspaceId &&
                t.ProjectId == theme.ProjectId &&
                string.Equals(t.ThemeKey, theme.ThemeKey, StringComparison.Ordinal)))
        
            throw new InvalidOperationException("ThemeKey already exists in scope: " + theme.ThemeKey);
        

        ProductLearningImprovementThemeRecord stored = new()
        {
            ThemeId = themeId,
            TenantId = theme.TenantId,
            WorkspaceId = theme.WorkspaceId,
            ProjectId = theme.ProjectId,
            ThemeKey = theme.ThemeKey,
            SourceAggregateKey = theme.SourceAggregateKey,
            PatternKey = theme.PatternKey,
            Title = theme.Title,
            Summary = theme.Summary,
            AffectedArtifactTypeOrWorkflowArea = theme.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = theme.SeverityBand,
            EvidenceSignalCount = theme.EvidenceSignalCount,
            DistinctRunCount = theme.DistinctRunCount,
            AverageTrustScore = theme.AverageTrustScore,
            DerivationRuleVersion = theme.DerivationRuleVersion,
            Status = status,
            CreatedUtc = createdUtc,
            CreatedByUserId = theme.CreatedByUserId
        };

        _themes.Add(stored);

        return Task.CompletedTask;
    }

    public Task<ProductLearningImprovementThemeRecord?> GetThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        ProductLearningImprovementThemeRecord? found = _themes.FirstOrDefault(t =>
            t.ThemeId == themeId &&
            t.TenantId == scope.TenantId &&
            t.WorkspaceId == scope.WorkspaceId &&
            t.ProjectId == scope.ProjectId);

        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<ProductLearningImprovementThemeRecord>> ListThemesAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);
        ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        List<ProductLearningImprovementThemeRecord> list = _themes
            .Where(t =>
                t.TenantId == scope.TenantId &&
                t.WorkspaceId == scope.WorkspaceId &&
                t.ProjectId == scope.ProjectId)
            .OrderByDescending(static t => t.CreatedUtc)
            .ThenBy(static t => t.ThemeId)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductLearningImprovementThemeRecord>>(list);
    }

    public Task InsertPlanAsync(ProductLearningImprovementPlanRecord plan, CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsurePlan(plan);

        string status = ProductLearningPlanningRepositoryValidation.NormalizePlanStatus(plan.Status);
        Guid planId = plan.PlanId == Guid.Empty ? Guid.NewGuid() : plan.PlanId;
        DateTime createdUtc = plan.CreatedUtc == default ? DateTime.UtcNow : plan.CreatedUtc;

        ProductLearningImprovementThemeRecord? theme = _themes.FirstOrDefault(t =>
            t.ThemeId == plan.ThemeId &&
            t.TenantId == plan.TenantId &&
            t.WorkspaceId == plan.WorkspaceId &&
            t.ProjectId == plan.ProjectId);

        if (theme is null)
        
            throw new InvalidOperationException("Theme not found for ThemeId=" + plan.ThemeId + ".");
        

        IReadOnlyList<ProductLearningImprovementPlanActionStep> stepsCopy = plan.ActionSteps
            .OrderBy(static s => s.Ordinal)
            .Select(static s => new ProductLearningImprovementPlanActionStep
            {
                Ordinal = s.Ordinal,
                ActionType = s.ActionType,
                Description = s.Description,
                AcceptanceCriteria = s.AcceptanceCriteria
            })
            .ToList();

        ProductLearningImprovementPlanRecord stored = new()
        {
            PlanId = planId,
            TenantId = plan.TenantId,
            WorkspaceId = plan.WorkspaceId,
            ProjectId = plan.ProjectId,
            ThemeId = plan.ThemeId,
            Title = plan.Title,
            Summary = plan.Summary,
            ActionSteps = stepsCopy,
            PriorityScore = plan.PriorityScore,
            PriorityExplanation = plan.PriorityExplanation,
            Status = status,
            CreatedUtc = createdUtc,
            CreatedByUserId = plan.CreatedByUserId
        };

        _plans.Add(stored);

        return Task.CompletedTask;
    }

    public Task<ProductLearningImprovementPlanRecord?> GetPlanAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        ProductLearningImprovementPlanRecord? found = _plans.FirstOrDefault(p =>
            p.PlanId == planId &&
            p.TenantId == scope.TenantId &&
            p.WorkspaceId == scope.WorkspaceId &&
            p.ProjectId == scope.ProjectId);

        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<ProductLearningImprovementPlanRecord>> ListPlansAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);
        ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        List<ProductLearningImprovementPlanRecord> list = _plans
            .Where(p =>
                p.TenantId == scope.TenantId &&
                p.WorkspaceId == scope.WorkspaceId &&
                p.ProjectId == scope.ProjectId)
            .OrderByDescending(static p => p.CreatedUtc)
            .ThenBy(static p => p.PlanId)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductLearningImprovementPlanRecord>>(list);
    }

    public Task<IReadOnlyList<ProductLearningImprovementPlanRecord>> ListPlansForThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);
        ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        List<ProductLearningImprovementPlanRecord> list = _plans
            .Where(p =>
                p.ThemeId == themeId &&
                p.TenantId == scope.TenantId &&
                p.WorkspaceId == scope.WorkspaceId &&
                p.ProjectId == scope.ProjectId)
            .OrderByDescending(static p => p.CreatedUtc)
            .ThenBy(static p => p.PlanId)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductLearningImprovementPlanRecord>>(list);
    }

    public Task AddPlanArchitectureRunLinkAsync(
        ProductLearningImprovementPlanRunLinkRecord link,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureRunLink(link);

        ProductLearningImprovementPlanRecord? plan = _plans.FirstOrDefault(p => p.PlanId == link.PlanId);

        if (plan is null)
        
            throw new InvalidOperationException("Plan not found for PlanId=" + link.PlanId + ".");
        

        if (_runLinks.Any(r =>
                r.PlanId == link.PlanId &&
                string.Equals(r.ArchitectureRunId, link.ArchitectureRunId, StringComparison.Ordinal)))
        
            throw new InvalidOperationException("Run link already exists for this plan.");
        

        _runLinks.Add(
            new ProductLearningImprovementPlanRunLinkRecord
            {
                PlanId = link.PlanId,
                ArchitectureRunId = link.ArchitectureRunId
            });

        return Task.CompletedTask;
    }

    public Task AddPlanSignalLinkAsync(
        ProductLearningImprovementPlanSignalLinkRecord link,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureSignalLink(link);

        ProductLearningImprovementPlanRecord? plan = _plans.FirstOrDefault(p => p.PlanId == link.PlanId);

        if (plan is null)
        
            throw new InvalidOperationException("Plan not found for PlanId=" + link.PlanId + ".");
        

        if (_signalLinks.Any(s => s.PlanId == link.PlanId && s.SignalId == link.SignalId))
        
            throw new InvalidOperationException("Signal link already exists for this plan.");
        

        _signalLinks.Add(
            new ProductLearningImprovementPlanSignalLinkRecord
            {
                PlanId = link.PlanId,
                SignalId = link.SignalId,
                TriageStatusSnapshot = link.TriageStatusSnapshot
            });

        return Task.CompletedTask;
    }

    public Task AddPlanArtifactLinkAsync(
        ProductLearningImprovementPlanArtifactLinkRecord link,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureArtifactLink(link);

        ProductLearningImprovementPlanRecord? plan = _plans.FirstOrDefault(p => p.PlanId == link.PlanId);

        if (plan is null)
        
            throw new InvalidOperationException("Plan not found for PlanId=" + link.PlanId + ".");
        

        Guid linkId = link.LinkId == Guid.Empty ? Guid.NewGuid() : link.LinkId;

        _artifactLinks.Add(
            new ProductLearningImprovementPlanArtifactLinkRecord
            {
                LinkId = linkId,
                PlanId = link.PlanId,
                AuthorityBundleId = link.AuthorityBundleId,
                AuthorityArtifactSortOrder = link.AuthorityArtifactSortOrder,
                PilotArtifactHint = link.PilotArtifactHint
            });

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListPlanArchitectureRunIdsAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        if (!_plans.Any(p =>
                p.PlanId == planId &&
                p.TenantId == scope.TenantId &&
                p.WorkspaceId == scope.WorkspaceId &&
                p.ProjectId == scope.ProjectId))
        
            return Task.FromResult<IReadOnlyList<string>>([]);
        

        List<string> ids = _runLinks
            .Where(r => r.PlanId == planId)
            .Select(static r => r.ArchitectureRunId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(ids);
    }

    public Task<IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord>> ListPlanSignalLinksAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        if (!_plans.Any(p =>
                p.PlanId == planId &&
                p.TenantId == scope.TenantId &&
                p.WorkspaceId == scope.WorkspaceId &&
                p.ProjectId == scope.ProjectId))
        
            return Task.FromResult<IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord>>(
                []);
        

        List<ProductLearningImprovementPlanSignalLinkRecord> list = _signalLinks
            .Where(s => s.PlanId == planId)
            .OrderBy(static s => s.SignalId)
            .Select(static s => new ProductLearningImprovementPlanSignalLinkRecord
            {
                PlanId = s.PlanId,
                SignalId = s.SignalId,
                TriageStatusSnapshot = s.TriageStatusSnapshot
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord>>(list);
    }

    public Task<IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord>> ListPlanArtifactLinksAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        if (!_plans.Any(p =>
                p.PlanId == planId &&
                p.TenantId == scope.TenantId &&
                p.WorkspaceId == scope.WorkspaceId &&
                p.ProjectId == scope.ProjectId))
        
            return Task.FromResult<IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord>>(
                []);
        

        List<ProductLearningImprovementPlanArtifactLinkRecord> list = _artifactLinks
            .Where(a => a.PlanId == planId)
            .OrderBy(static a => a.LinkId)
            .Select(static a => new ProductLearningImprovementPlanArtifactLinkRecord
            {
                LinkId = a.LinkId,
                PlanId = a.PlanId,
                AuthorityBundleId = a.AuthorityBundleId,
                AuthorityArtifactSortOrder = a.AuthorityArtifactSortOrder,
                PilotArtifactHint = a.PilotArtifactHint
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord>>(list);
    }
}
