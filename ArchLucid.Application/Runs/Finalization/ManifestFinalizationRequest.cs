using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Decisioning.Interfaces;

using Cm = ArchLucid.Contracts.Manifest;
using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Application.Runs.Finalization;

/// <summary>Inputs for <see cref="IManifestFinalizationService.FinalizeAsync" /> (authority commit phase).</summary>
public sealed class ManifestFinalizationRequest
{
    public required Guid RunId
    {
        get;
        init;
    }

    /// <summary>Must match <c>dbo.Runs.FindingsSnapshotId</c> at finalization time.</summary>
    public required Guid ExpectedFindingsSnapshotId
    {
        get;
        init;
    }

    /// <summary>
    ///     When non-null, must match <c>dbo.Runs.ArtifactBundleId</c> (pipeline linked artifacts). When the run has no bundle
    ///     yet, leave null.
    /// </summary>
    public Guid? ExpectedArtifactBundleId
    {
        get;
        init;
    }

    public required string ActorUserId
    {
        get;
        init;
    }

    public required string ActorUserName
    {
        get;
        init;
    }

    public string? CorrelationId
    {
        get;
        init;
    }

    public required Dm.ManifestDocument ManifestModel
    {
        get;
        init;
    }

    public required Cm.GoldenManifest Contract
    {
        get;
        init;
    }

    public required SaveContractsManifestOptions Keying
    {
        get;
        init;
    }

    public required DecisionTrace Trace
    {
        get;
        init;
    }
}
