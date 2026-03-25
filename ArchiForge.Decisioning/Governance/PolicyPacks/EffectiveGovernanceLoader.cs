using ArchiForge.Decisioning.Governance.Resolution;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Default implementation of <see cref="IEffectiveGovernanceLoader"/> that forwards to <see cref="IEffectiveGovernanceResolver"/>.
/// </summary>
/// <remarks>
/// Registered scoped alongside <see cref="IEffectiveGovernanceResolver"/> in the API host. Injected by alert, advisory, and compliance
/// services that only need the flattened effective document.
/// </remarks>
/// <param name="resolver">Resolver that performs full merge and diagnostics.</param>
public sealed class EffectiveGovernanceLoader(IEffectiveGovernanceResolver resolver) : IEffectiveGovernanceLoader
{
    /// <inheritdoc />
    /// <remarks>
    /// Calls <see cref="IEffectiveGovernanceResolver.ResolveAsync"/> and discards <see cref="EffectiveGovernanceResolutionResult.Decisions"/>,
    /// <see cref="EffectiveGovernanceResolutionResult.Conflicts"/>, and <see cref="EffectiveGovernanceResolutionResult.Notes"/>.
    /// </remarks>
    public async Task<PolicyPackContentDocument> LoadEffectiveContentAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        EffectiveGovernanceResolutionResult result = await resolver
            .ResolveAsync(tenantId, workspaceId, projectId, ct)
            .ConfigureAwait(false);

        return result.EffectiveContent;
    }
}
