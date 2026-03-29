using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.ContextIngestion.Infrastructure;

/// <summary>
/// Defines the fixed <see cref="IContextConnector"/> execution order for context ingestion.
/// This is the single code location for that order; keep <c>docs/CONTEXT_INGESTION.md</c> aligned.
/// </summary>
/// <remarks>
/// <see cref="ContextIngestion.Services.ContextIngestionService"/> resolves
/// <see cref="IEnumerable{T}"/> of <see cref="IContextConnector"/> only from the registration that
/// calls <see cref="ResolveOrdered"/> — not from unconstrained multi-registration, so order is never
/// left to implicit collection behavior.
/// </remarks>
public static class ContextConnectorPipeline
{
    /// <summary>
    /// Resolves connectors in pipeline order (fetch → normalize → delta per connector, in this sequence).
    /// </summary>
    public static IReadOnlyList<IContextConnector> ResolveOrdered(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return
        [
            services.GetRequiredService<StaticRequestContextConnector>(),
            services.GetRequiredService<InlineRequirementsConnector>(),
            services.GetRequiredService<DocumentConnector>(),
            services.GetRequiredService<PolicyReferenceConnector>(),
            services.GetRequiredService<TopologyHintsConnector>(),
            services.GetRequiredService<SecurityBaselineHintsConnector>(),
            services.GetRequiredService<InfrastructureDeclarationConnector>()
        ];
    }
}
