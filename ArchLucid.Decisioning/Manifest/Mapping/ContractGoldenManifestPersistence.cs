using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;

using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Manifest.Mapping;

/// <summary>Shared contract-save path for <see cref="IGoldenManifestRepository.SaveAsync(Cm.GoldenManifest,...)" />.</summary>
public static class ContractGoldenManifestPersistence
{
    /// <summary>
    ///     Builds the authority <see cref="GoldenManifest" /> row for insert: either from the coordinator projection mapper
    ///     or by validating and scoping a caller-supplied engine body (PR A2 authority commit).
    /// </summary>
    public static GoldenManifest ResolveGoldenManifestForContractSave(
        Cm.GoldenManifest contract,
        ScopeContext scope,
        SaveContractsManifestOptions keying,
        GoldenManifest? authorityPersistBody)
    {
        if (authorityPersistBody is null)
            return ContractGoldenManifestMapper.ToAuthorityModel(contract, scope, keying);

        if (authorityPersistBody.ManifestId != keying.ManifestId)
            throw new ArgumentException("authorityPersistBody.ManifestId must match keying.ManifestId.",
                nameof(authorityPersistBody));

        if (authorityPersistBody.RunId != keying.RunId)
            throw new ArgumentException("authorityPersistBody.RunId must match keying.RunId.",
                nameof(authorityPersistBody));

        if (authorityPersistBody.ContextSnapshotId != keying.ContextSnapshotId)
            throw new ArgumentException("authorityPersistBody.ContextSnapshotId must match keying.ContextSnapshotId.",
                nameof(authorityPersistBody));

        if (authorityPersistBody.GraphSnapshotId != keying.GraphSnapshotId)
            throw new ArgumentException("authorityPersistBody.GraphSnapshotId must match keying.GraphSnapshotId.",
                nameof(authorityPersistBody));

        if (authorityPersistBody.FindingsSnapshotId != keying.FindingsSnapshotId)
            throw new ArgumentException("authorityPersistBody.FindingsSnapshotId must match keying.FindingsSnapshotId.",
                nameof(authorityPersistBody));

        if (authorityPersistBody.DecisionTraceId != keying.DecisionTraceId)
            throw new ArgumentException("authorityPersistBody.DecisionTraceId must match keying.DecisionTraceId.",
                nameof(authorityPersistBody));

        authorityPersistBody.TenantId = scope.TenantId;
        authorityPersistBody.WorkspaceId = scope.WorkspaceId;
        authorityPersistBody.ProjectId = scope.ProjectId;
        return authorityPersistBody;
    }
}
