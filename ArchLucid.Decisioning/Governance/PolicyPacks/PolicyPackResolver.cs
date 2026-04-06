using ArchiForge.Decisioning.Governance.Resolution;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Default <see cref="IPolicyPackResolver"/>: loads hierarchical assignments, filters <see cref="PolicyPackAssignment.IsEnabled"/>,
/// and attaches each pack’s <see cref="PolicyPackVersion.ContentJson"/>.
/// </summary>
/// <remarks>
/// Differs from <see cref="IEffectiveGovernanceResolver"/> in that it does not merge IDs/dictionaries or emit decisions/conflicts.
/// Used for operator visibility of “which packs are attached” (see HTTP effective-set endpoint).
/// </remarks>
public sealed class PolicyPackResolver(
    IPolicyPackAssignmentRepository assignmentRepository,
    IPolicyPackRepository packRepository,
    IPolicyPackVersionRepository versionRepository) : IPolicyPackResolver
{
    /// <inheritdoc />
    /// <remarks>
    /// Iterates assignments in repository order (typically <see cref="PolicyPackAssignment.AssignedUtc"/> descending).
    /// Missing <see cref="PolicyPack"/> or <see cref="PolicyPackVersion"/> causes that assignment to be skipped (orphan-safe).
    /// </remarks>
    public async Task<EffectivePolicyPackSet> ResolveAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        IReadOnlyList<PolicyPackAssignment> assignments = await assignmentRepository
            .ListByScopeAsync(tenantId, workspaceId, projectId, ct)
            ;

        EffectivePolicyPackSet result = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
        };

        foreach (PolicyPackAssignment assignment in assignments.Where(x => x.IsEnabled))
        {
            PolicyPack? pack = await packRepository.GetByIdAsync(assignment.PolicyPackId, ct);
            if (pack is null)
                continue;

            PolicyPackVersion? version = await versionRepository
                .GetByPackAndVersionAsync(assignment.PolicyPackId, assignment.PolicyPackVersion, ct)
                ;

            if (version is null)
                continue;

            result.Packs.Add(
                new ResolvedPolicyPack
                {
                    PolicyPackId = pack.PolicyPackId,
                    Name = pack.Name,
                    Version = version.Version,
                    PackType = pack.PackType,
                    ContentJson = version.ContentJson,
                });
        }

        return result;
    }
}
