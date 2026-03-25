using System.Transactions;

using ArchiForge.Decisioning.Governance.Resolution;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Default <see cref="IPolicyPackManagementService"/> implementation: orchestrates repositories for create / publish / assign flows.
/// </summary>
public sealed class PolicyPackManagementService(
    IPolicyPackRepository packRepository,
    IPolicyPackVersionRepository versionRepository,
    IPolicyPackAssignmentRepository assignmentRepository) : IPolicyPackManagementService
{
    /// <inheritdoc />
    public async Task<PolicyPack> CreatePackAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string name,
        string description,
        string packType,
        string initialContentJson,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(packType);

        PolicyPack pack = new()
        {
            PolicyPackId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            Name = name,
            Description = description,
            PackType = packType,
            Status = PolicyPackStatus.Draft,
            CreatedUtc = DateTime.UtcNow,
            CurrentVersion = "1.0.0",
        };

        using (TransactionScope scope = new(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled))
        {
            await packRepository.CreateAsync(pack, ct).ConfigureAwait(false);

            await versionRepository
                .CreateAsync(
                    new PolicyPackVersion
                    {
                        PolicyPackVersionId = Guid.NewGuid(),
                        PolicyPackId = pack.PolicyPackId,
                        Version = "1.0.0",
                        ContentJson = string.IsNullOrWhiteSpace(initialContentJson) ? "{}" : initialContentJson,
                        CreatedUtc = DateTime.UtcNow,
                        IsPublished = false,
                    },
                    ct)
                .ConfigureAwait(false);

            scope.Complete();
        }

        return pack;
    }

    /// <inheritdoc />
    public async Task<PolicyPackVersion> PublishVersionAsync(
        Guid policyPackId,
        string version,
        string contentJson,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        string normalizedJson = string.IsNullOrWhiteSpace(contentJson) ? "{}" : contentJson;

        PolicyPackVersion? existing = await versionRepository
            .GetByPackAndVersionAsync(policyPackId, version, ct)
            .ConfigureAwait(false);

        PolicyPackVersion packVersion;
        if (existing is not null)
        {
            existing.ContentJson = normalizedJson;
            existing.IsPublished = true;
            await versionRepository.UpdateAsync(existing, ct).ConfigureAwait(false);
            packVersion = existing;
        }
        else
        {
            packVersion = new PolicyPackVersion
            {
                PolicyPackVersionId = Guid.NewGuid(),
                PolicyPackId = policyPackId,
                Version = version,
                ContentJson = normalizedJson,
                CreatedUtc = DateTime.UtcNow,
                IsPublished = true,
            };

            await versionRepository.CreateAsync(packVersion, ct).ConfigureAwait(false);
        }

        PolicyPack? pack = await packRepository.GetByIdAsync(policyPackId, ct).ConfigureAwait(false);
        if (pack is not null)
        {
            pack.CurrentVersion = version;
            pack.Status = PolicyPackStatus.Active;
            pack.ActivatedUtc = DateTime.UtcNow;
            await packRepository.UpdateAsync(pack, ct).ConfigureAwait(false);
        }

        return packVersion;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Persists <see cref="GovernanceScopeLevel"/>-appropriate workspace/project ids (empty GUIDs when not part of the tier)
    /// so <see cref="IPolicyPackAssignmentRepository.ListByScopeAsync"/> can match tenant-wide and workspace-wide rows.
    /// </remarks>
    public async Task<PolicyPackAssignment> AssignAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid policyPackId,
        string version,
        string scopeLevel,
        bool isPinned,
        CancellationToken ct)
    {
        string normalized = GovernanceScopeLevel.TryNormalize(scopeLevel) ?? GovernanceScopeLevel.Project;

        Guid ws = workspaceId;
        Guid proj = projectId;
        if (string.Equals(normalized, GovernanceScopeLevel.Tenant, StringComparison.Ordinal))
        {
            ws = Guid.Empty;
            proj = Guid.Empty;
        }
        else if (string.Equals(normalized, GovernanceScopeLevel.Workspace, StringComparison.Ordinal))
        {
            proj = Guid.Empty;
        }

        PolicyPackAssignment assignment = new()
        {
            AssignmentId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = ws,
            ProjectId = proj,
            PolicyPackId = policyPackId,
            PolicyPackVersion = version,
            IsEnabled = true,
            ScopeLevel = normalized,
            IsPinned = isPinned,
            AssignedUtc = DateTime.UtcNow,
        };

        await assignmentRepository.CreateAsync(assignment, ct).ConfigureAwait(false);
        return assignment;
    }
}
