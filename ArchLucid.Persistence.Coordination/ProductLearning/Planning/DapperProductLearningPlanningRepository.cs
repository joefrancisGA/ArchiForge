using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Connections;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

/// <summary>Dapper access to 59R planning bridge tables.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperProductLearningPlanningRepository(ISqlConnectionFactory connectionFactory)
    : IProductLearningPlanningRepository
{
    private readonly DapperProductLearningPlanningPlanLinkRepository _links = new(connectionFactory);
    private readonly DapperProductLearningPlanningPlanRepository _plans = new(connectionFactory);
    private readonly DapperProductLearningPlanningThemeRepository _themes = new(connectionFactory);

    public Task InsertThemeAsync(ProductLearningImprovementThemeRecord theme, CancellationToken cancellationToken)
    {
        return _themes.InsertThemeAsync(theme, cancellationToken);
    }

    public Task<ProductLearningImprovementThemeRecord?> GetThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        return _themes.GetThemeAsync(themeId, scope, cancellationToken);
    }

    public Task<IReadOnlyList<ProductLearningImprovementThemeRecord>> ListThemesAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        return _themes.ListThemesAsync(scope, take, cancellationToken);
    }

    public Task InsertPlanAsync(ProductLearningImprovementPlanRecord plan, CancellationToken cancellationToken)
    {
        return _plans.InsertPlanAsync(plan, cancellationToken);
    }

    public Task<ProductLearningImprovementPlanRecord?> GetPlanAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        return _plans.GetPlanAsync(planId, scope, cancellationToken);
    }

    public Task<IReadOnlyList<ProductLearningImprovementPlanRecord>> ListPlansAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        return _plans.ListPlansAsync(scope, take, cancellationToken);
    }

    public Task<IReadOnlyList<ProductLearningImprovementPlanRecord>> ListPlansForThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        return _plans.ListPlansForThemeAsync(themeId, scope, take, cancellationToken);
    }

    public Task AddPlanArchitectureRunLinkAsync(
        ProductLearningImprovementPlanRunLinkRecord link,
        CancellationToken cancellationToken)
    {
        return _links.AddPlanArchitectureRunLinkAsync(link, cancellationToken);
    }

    public Task AddPlanSignalLinkAsync(
        ProductLearningImprovementPlanSignalLinkRecord link,
        CancellationToken cancellationToken)
    {
        return _links.AddPlanSignalLinkAsync(link, cancellationToken);
    }

    public Task AddPlanArtifactLinkAsync(
        ProductLearningImprovementPlanArtifactLinkRecord link,
        CancellationToken cancellationToken)
    {
        return _links.AddPlanArtifactLinkAsync(link, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListPlanArchitectureRunIdsAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        return _links.ListPlanArchitectureRunIdsAsync(planId, scope, cancellationToken);
    }

    public Task<IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord>> ListPlanSignalLinksAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        return _links.ListPlanSignalLinksAsync(planId, scope, cancellationToken);
    }

    public Task<IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord>> ListPlanArtifactLinksAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        return _links.ListPlanArtifactLinksAsync(planId, scope, cancellationToken);
    }
}
