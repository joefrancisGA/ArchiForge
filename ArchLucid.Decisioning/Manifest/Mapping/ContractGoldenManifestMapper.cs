using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest.Sections;

using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Manifest.Mapping;

/// <summary>Maps a coordinator-shaped <see cref="Cm.GoldenManifest"/> into an authority <see cref="Models.GoldenManifest"/> for persistence.</summary>
public static class ContractGoldenManifestMapper
{
    public static ArchLucid.Decisioning.Models.GoldenManifest ToAuthorityModel(
        Cm.GoldenManifest contract,
        ScopeContext scope,
        SaveContractsManifestOptions keying)
    {
        if (contract is null)
            throw new ArgumentNullException(nameof(contract));

        if (scope is null)
            throw new ArgumentNullException(nameof(scope));

        if (keying is null)
            throw new ArgumentNullException(nameof(keying));

        ArchLucid.Decisioning.Models.GoldenManifest model = new()
        {
            SchemaVersion = 1,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ManifestId = keying.ManifestId,
            RunId = keying.RunId,
            ContextSnapshotId = keying.ContextSnapshotId,
            GraphSnapshotId = keying.GraphSnapshotId,
            FindingsSnapshotId = keying.FindingsSnapshotId,
            DecisionTraceId = keying.DecisionTraceId,
            CreatedUtc = keying.CreatedUtc,
            ManifestHash = string.Empty,
            RuleSetId = keying.RuleSetId,
            RuleSetVersion = keying.RuleSetVersion,
            RuleSetHash = keying.RuleSetHash,
            Metadata = new ManifestMetadata
            {
                Name = contract.SystemName,
                Version = contract.Metadata.ManifestVersion,
                Status = "Draft",
                Summary = contract.Metadata.ChangeDescription,
            },
            Topology = { Services = [.. contract.Services], Datastores = [.. contract.Datastores], Resources =
                [.. contract.Services.Select(s => s.ServiceName)
                    .Concat(contract.Datastores.Select(d => d.DatastoreName))]
            },
            Security = { Controls =
                [.. contract.Services.SelectMany(
                    s => s.RequiredControls.Select(
                        c => new SecurityPostureItem
                        {
                            ControlName = c,
                            Status = "stated",
                            ControlId = c,
                            Impact = string.Empty,
                        }))]
            },
            Compliance = { Controls =
                [.. contract.Governance.ComplianceTags
                    .Select(
                        t => new CompliancePostureItem
                        {
                            ControlName = t,
                            ControlId = t,
                            AppliesToCategory = "governance",
                            Status = "Tagged",
                        })]
            },
            Policy = { Notes = [.. contract.Governance.PolicyConstraints] }
        };

        return model;
    }
}
