using System.Data;
using System.Text.Json;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Governance.Resolution;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Default <see cref="IPolicyPackManagementService"/> implementation: orchestrates repositories for create / publish / assign flows.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Change log:</strong> <see cref="IPolicyPackChangeLogRepository"/> receives append-only rows for mutations.
/// <see cref="CreatePackAsync"/> uses <c>ChangedBy = "system"</c> because the service has no caller identity parameter yet.
/// </para>
/// <para>
/// <strong>Create + durability:</strong> The pack/version rows commit first; the change log append runs afterward on the
/// repository default connection so a failed log insert cannot roll back the primary mutation (see <see cref="AppendChangeLogAsync"/>).
/// </para>
/// </remarks>
public sealed class PolicyPackManagementService(
    IPolicyPackRepository packRepository,
    IPolicyPackVersionRepository versionRepository,
    IPolicyPackAssignmentRepository assignmentRepository,
    IPolicyPackChangeLogRepository changeLogRepository,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
    ILogger<PolicyPackManagementService> logger) : IPolicyPackManagementService
{
    private const string InitialVersion = "1.0.0";

    private static readonly JsonSerializerOptions ChangeLogJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

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

        string newValueJson = JsonSerializer.Serialize(
            new { name, description, packType, initialVersion = InitialVersion },
            ChangeLogJsonOptions);

        await AppendChangeLogAsync(
            pack.PolicyPackId,
            pack.TenantId,
            pack.WorkspaceId,
            pack.ProjectId,
            PolicyPackChangeTypes.Created,
            "system",
            previousValue: null,
            newValue: newValueJson,
            summaryText: $"Policy pack '{name}' created with initial version {InitialVersion}.",
            ct,
            connection: null,
            transaction: null);

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

        (PolicyPackVersion packVersion, string? previousValue) =
            await versionRepository.UpsertPublishedVersionAsync(policyPackId, version, normalizedJson, ct);

        PolicyPack pack = await packRepository.GetByIdAsync(policyPackId, ct) ?? throw new InvalidOperationException(
                $"Policy pack '{policyPackId}' was not found. Cannot promote version '{version}' on a non-existent pack.");

        pack.CurrentVersion = version;
        pack.Status = PolicyPackStatus.Active;
        pack.ActivatedUtc = DateTime.UtcNow;
        await packRepository.UpdateAsync(pack, ct);

        await AppendChangeLogAsync(
            policyPackId,
            pack.TenantId,
            pack.WorkspaceId,
            pack.ProjectId,
            PolicyPackChangeTypes.VersionPublished,
            "system",
            previousValue,
            newValue: normalizedJson,
            summaryText: $"Version '{version}' published for pack '{policyPackId}'.",
            ct,
            connection: null,
            transaction: null);

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

        string assignJson = JsonSerializer.Serialize(
            new { scopeLevel = normalized, version, isPinned },
            ChangeLogJsonOptions);

        await AppendChangeLogAsync(
            policyPackId,
            tenantId,
            ws,
            proj,
            PolicyPackChangeTypes.Assigned,
            "system",
            previousValue: null,
            newValue: assignJson,
            summaryText: $"Pack '{policyPackId}' assigned at {normalized} scope, version '{version}'.",
            ct,
            connection: null,
            transaction: null);

        return assignment;
    }

    /// <inheritdoc />
    public async Task<bool> TryArchiveAssignmentAsync(Guid tenantId, Guid assignmentId, CancellationToken ct)
    {
        bool ok = await assignmentRepository.ArchiveAsync(tenantId, assignmentId, ct);
        if (!ok)
            return false;

        PolicyPackAssignment? row = await assignmentRepository.GetByTenantAndAssignmentIdAsync(tenantId, assignmentId, ct);
        if (row is null)
            return true;

        await AppendChangeLogAsync(
            row.PolicyPackId,
            row.TenantId,
            row.WorkspaceId,
            row.ProjectId,
            PolicyPackChangeTypes.AssignmentArchived,
            "system",
            previousValue: null,
            newValue: null,
            summaryText: $"Assignment '{assignmentId}' archived.",
            ct,
            connection: null,
            transaction: null);

        return true;
    }

    private async Task AppendChangeLogAsync(
        Guid policyPackId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string changeType,
        string changedBy,
        string? previousValue,
        string? newValue,
        string? summaryText,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryText);

        PolicyPackChangeLogEntry entry = new()
        {
            PolicyPackId = policyPackId,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            ChangeType = changeType,
            ChangedBy = changedBy,
            ChangedUtc = DateTime.UtcNow,
            PreviousValue = previousValue,
            NewValue = newValue,
            SummaryText = summaryText,
        };

        try
        {
            await changeLogRepository.AppendAsync(entry, ct, connection, transaction);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Policy pack change log append failed for PolicyPackId={PolicyPackId}, ChangeType={ChangeType}. Primary mutation already completed.",
                policyPackId,
                changeType);
        }
    }
}
