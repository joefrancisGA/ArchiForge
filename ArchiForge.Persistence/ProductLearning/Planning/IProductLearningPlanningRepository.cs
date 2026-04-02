using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;

namespace ArchiForge.Persistence.ProductLearning.Planning;

/// <summary>
/// Persistence for 59R learning-to-planning bridge (themes, bounded plans, explicit links). No generation-side mutation.
/// </summary>
public interface IProductLearningPlanningRepository
{
    /// <summary>Inserts a theme. Assigns <see cref="ProductLearningImprovementThemeRecord.ThemeId"/> and <see cref="ProductLearningImprovementThemeRecord.CreatedUtc"/> when unset/default.</summary>
    Task InsertThemeAsync(ProductLearningImprovementThemeRecord theme, CancellationToken cancellationToken);

    Task<ProductLearningImprovementThemeRecord?> GetThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);

    /// <summary>Newest first; stable tie-break on <see cref="ProductLearningImprovementThemeRecord.ThemeId"/> ascending.</summary>
    Task<IReadOnlyList<ProductLearningImprovementThemeRecord>> ListThemesAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken);

    /// <summary>Inserts a plan. Assigns <see cref="ProductLearningImprovementPlanRecord.PlanId"/> and <see cref="ProductLearningImprovementPlanRecord.CreatedUtc"/> when unset/default.</summary>
    Task InsertPlanAsync(ProductLearningImprovementPlanRecord plan, CancellationToken cancellationToken);

    Task<ProductLearningImprovementPlanRecord?> GetPlanAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);

    /// <summary>Newest first; stable tie-break on <see cref="ProductLearningImprovementPlanRecord.PlanId"/> ascending.</summary>
    Task<IReadOnlyList<ProductLearningImprovementPlanRecord>> ListPlansAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductLearningImprovementPlanRecord>> ListPlansForThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken);

    Task AddPlanArchitectureRunLinkAsync(
        ProductLearningImprovementPlanRunLinkRecord link,
        CancellationToken cancellationToken);

    Task AddPlanSignalLinkAsync(
        ProductLearningImprovementPlanSignalLinkRecord link,
        CancellationToken cancellationToken);

    Task AddPlanArtifactLinkAsync(
        ProductLearningImprovementPlanArtifactLinkRecord link,
        CancellationToken cancellationToken);

    /// <summary>Deterministic order: <c>ArchitectureRunId</c> ascending.</summary>
    Task<IReadOnlyList<string>> ListPlanArchitectureRunIdsAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);

    /// <summary>Deterministic order: <see cref="ProductLearningImprovementPlanSignalLinkRecord.SignalId"/> ascending.</summary>
    Task<IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord>> ListPlanSignalLinksAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);

    /// <summary>Deterministic order: <see cref="ProductLearningImprovementPlanArtifactLinkRecord.LinkId"/> ascending.</summary>
    Task<IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord>> ListPlanArtifactLinksAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken);
}
