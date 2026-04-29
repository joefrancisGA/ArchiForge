using System.Data;



using ArchLucid.ArtifactSynthesis.Interfaces;

using ArchLucid.ArtifactSynthesis.Models;

using ArchLucid.Core.Scoping;



namespace ArchLucid.ArtifactSynthesis.Repositories;



public class InMemoryArtifactBundleRepository : IArtifactBundleRepository

{

    private const int MaxEntries = 500;



    private readonly Lock _lock = new();



    private readonly List<ArtifactBundle> _store = [];



    public Task SaveAsync(

        ArtifactBundle bundle,

        CancellationToken ct,

        IDbConnection? connection = null,

        IDbTransaction? transaction = null)

    {

        ct.ThrowIfCancellationRequested();

        _ = connection;

        _ = transaction;

        lock (_lock)

        {

            _store.Add(bundle);

            if (_store.Count > MaxEntries)



                _store.RemoveRange(0, _store.Count - MaxEntries);

        }



        return Task.CompletedTask;

    }



    public Task<ArtifactBundle?> GetByManifestIdAsync(

        ScopeContext scope,

        Guid manifestId,

        bool loadArtifactBodies,

        CancellationToken ct)

    {

        _ = ct;



        ArtifactBundle? result;

        lock (_lock)



        {

            result = _store.LastOrDefault(x =>

                x.ManifestId == manifestId &&

                x.TenantId == scope.TenantId &&

                x.WorkspaceId == scope.WorkspaceId &&

                x.ProjectId == scope.ProjectId);

        }



        if (result is null)

            return Task.FromResult<ArtifactBundle?>(null);



        if (loadArtifactBodies)

            return Task.FromResult<ArtifactBundle?>(result);



        return Task.FromResult<ArtifactBundle?>(WithBodiesStrippedSnapshot(result));

    }



    /// <summary>

    ///     Returns a detached copy whose artifact bodies are empty so callers never mutate stored bundles.

    /// </summary>

    private static ArtifactBundle WithBodiesStrippedSnapshot(ArtifactBundle source)

    {

        return new ArtifactBundle

        {

            TenantId = source.TenantId,

            WorkspaceId = source.WorkspaceId,

            ProjectId = source.ProjectId,

            BundleId = source.BundleId,

            RunId = source.RunId,

            ManifestId = source.ManifestId,

            CreatedUtc = source.CreatedUtc,

            Trace = source.Trace,

            Artifacts = source.Artifacts.ConvertAll(CloneArtifactWithEmptyBody)

        };

    }



    private static SynthesizedArtifact CloneArtifactWithEmptyBody(SynthesizedArtifact a)

    {

        return new SynthesizedArtifact

        {

            ArtifactId = a.ArtifactId,

            RunId = a.RunId,

            ManifestId = a.ManifestId,

            CreatedUtc = a.CreatedUtc,

            ArtifactType = a.ArtifactType,

            Name = a.Name,

            Format = a.Format,

            Content = string.Empty,

            ContentHash = a.ContentHash,

            Metadata = new Dictionary<string, string>(a.Metadata, StringComparer.Ordinal),

            ContributingDecisionIds = [.. a.ContributingDecisionIds]

        };

    }

}

