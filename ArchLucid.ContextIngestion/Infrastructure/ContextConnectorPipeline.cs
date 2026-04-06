using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.ContextIngestion.Infrastructure;

/// <summary>
/// Canonical definition of the context-ingestion <see cref="IContextConnector"/> pipeline order.
/// </summary>
/// <remarks>
/// <para><b>Why order matters:</b> <see cref="Services.ContextIngestionService"/> iterates this sequence
/// for every ingest. Each connector contributes canonical objects and a delta segment; segments are
/// concatenated into <see cref="Models.ContextSnapshot.DeltaSummary"/> (see
/// <see cref="Summaries.IContextDeltaSummaryBuilder"/>). Operators and support staff read that string as a
/// stable narrative of what changed—reordering connectors changes segment order and wording expectations
/// without changing underlying object merge semantics (enrichment and deduplication run after all connectors).</para>
/// <para><b>Single source of truth:</b> The API host registers <c>IEnumerable&lt;IContextConnector&gt;</c>
/// only via <see cref="CreateOrderedContextConnectorPipeline"/> so execution order is never inferred from
/// incidental <c>AddSingleton</c> registration order or multi-registration collection rules.</para>
/// <para>Keep <c>docs/CONTEXT_INGESTION.md</c> (numbered pipeline list) aligned with the sequence below.</para>
/// </remarks>
public static class ContextConnectorPipeline
{
    /// <summary>
    /// Builds the ordered connector list for DI. Call only from composition root registration; do not
    /// duplicate this sequence elsewhere.
    /// </summary>
    /// <param name="services">Service provider with all concrete connector types registered.</param>
    /// <returns>Connectors in pipeline order (fetch → normalize → delta per instance, in this order).</returns>
    public static IReadOnlyList<IContextConnector> CreateOrderedContextConnectorPipeline(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return
        [
            // 1 — Primary description → Requirement ("Primary Request").
            services.GetRequiredService<StaticRequestContextConnector>(),
            // 2 — Inline requirement strings → Requirements.
            services.GetRequiredService<InlineRequirementsConnector>(),
            // 3 — Pasted documents → parsed canonical objects (see IContextDocumentParser).
            services.GetRequiredService<DocumentConnector>(),
            // 4 — Policy reference strings → PolicyControl.
            services.GetRequiredService<PolicyReferenceConnector>(),
            // 5 — Topology hints → TopologyResource.
            services.GetRequiredService<TopologyHintsConnector>(),
            // 6 — Security baseline hints → SecurityBaseline.
            services.GetRequiredService<SecurityBaselineHintsConnector>(),
            // 7 — Structured IaC snippets (json / simple-terraform).
            services.GetRequiredService<InfrastructureDeclarationConnector>()
        ];
    }
}
