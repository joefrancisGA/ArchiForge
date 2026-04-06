using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Persistence.Alerts.Helpers;

/// <summary>
/// Resolves the effective governance document for an alert evaluation, reusing
/// any already-loaded content on the context to avoid redundant I/O.
/// </summary>
/// <remarks>
/// Both <see cref="ArchiForge.Persistence.Alerts.AlertService"/> and
/// <see cref="ArchiForge.Persistence.Alerts.CompositeAlertService"/> share identical
/// resolution logic; this helper centralises it so a future interface change only
/// requires one update.
/// </remarks>
internal static class AlertGovernanceResolver
{
    /// <summary>
    /// Returns <see cref="AlertEvaluationContext.EffectiveGovernanceContent"/> when present,
    /// otherwise loads it via <paramref name="loader"/>.
    /// </summary>
    internal static async Task<PolicyPackContentDocument> ResolveAsync(
        AlertEvaluationContext context,
        IEffectiveGovernanceLoader loader,
        CancellationToken ct)
    {
        if (context.EffectiveGovernanceContent is not null)
            return context.EffectiveGovernanceContent;

        return await loader
            .LoadEffectiveContentAsync(context.TenantId, context.WorkspaceId, context.ProjectId, ct)
            ;
    }
}
