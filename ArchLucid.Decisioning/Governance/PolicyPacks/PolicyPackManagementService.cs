using System.Data;

using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Governance.Resolution;

namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Default <see cref="IPolicyPackManagementService"/> implementation: orchestrates repositories for create / publish / assign flows.
/// </summary>
public sealed class PolicyPackManagementService(
    IPolicyPackRepository packRepository,
    IPolicyPackVersionRepository versionRepository,
    IPolicyPackAssignmentRepository assignmentRepository,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory) : IPolicyPackManagementService
{
    private const string InitialVersion = "1.0.0";
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
            CurrentVersion = InitialVersion,
        };

        await using IArchLucidUnitOfWork uow = await unitOfWorkFactory.CreateAsync(ct);

        try
        {
            if (uow.SupportsExternalTransaction)
            {
                await packRepository.CreateAsync(pack, ct, uow.Connection, uow.Transaction);

                await versionRepository
                    .CreateAsync(
                        new PolicyPackVersion
                        {
                            PolicyPackVersionId = Guid.NewGuid(),
                            PolicyPackId = pack.PolicyPackId,
                            Version = InitialVersion,
                            ContentJson = string.IsNullOrWhiteSpace(initialContentJson) ? "{}" : initialContentJson,
                            CreatedUtc = DateTime.UtcNow,
                            IsPublished = false,
                        },
                        ct,
                        uow.Connection,
                        uow.Transaction);
            }
            else
            {
                await packRepository.CreateAsync(pack, ct);

                await versionRepository
                    .CreateAsync(
                        new PolicyPackVersion
                        {
                            PolicyPackVersionId = Guid.NewGuid(),
                            PolicyPackId = pack.PolicyPackId,
                            Version = InitialVersion,
                            ContentJson = string.IsNullOrWhiteSpace(initialContentJson) ? "{}" : initialContentJson,
                            CreatedUtc = DateTime.UtcNow,
                            IsPublished = false,
                        },
                        ct);
            }

            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
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
            ;

        PolicyPackVersion packVersion;
        if (existing is not null)
        {
            existing.ContentJson = normalizedJson;
            existing.IsPublished = true;
            await versionRepository.UpdateAsync(existing, ct);
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

            await versionRepository.CreateAsync(packVersion, ct);
        }

        PolicyPack pack = await packRepository.GetByIdAsync(policyPackId, ct) ?? throw new InvalidOperationException(
                $"Policy pack '{policyPackId}' was not found. Cannot promote version '{version}' on a non-existent pack.");

        pack.CurrentVersion = version;
        pack.Status = PolicyPackStatus.Active;
        pack.ActivatedUtc = DateTime.UtcNow;
        await packRepository.UpdateAsync(pack, ct);

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
        
            proj = Guid.Empty;
        

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

        await assignmentRepository.CreateAsync(assignment, ct);
        return assignment;
    }

    /// <inheritdoc />
    public Task<bool> TryArchiveAssignmentAsync(Guid tenantId, Guid assignmentId, CancellationToken ct) =>
        assignmentRepository.ArchiveAsync(tenantId, assignmentId, ct);
}
