using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Queries;

namespace ArchiForge.Persistence.Replay;

/// <summary>
/// <see cref="IAuthorityReplayService"/> implementation: validate stored run, optionally rebuild manifest/trace and artifacts under scope derived from the run row.
/// </summary>
/// <remarks>
/// Read path uses <see cref="IScopeContextProvider.GetCurrentScope"/>; writes use <see cref="RunRecord"/> tenant/workspace/<c>ScopeProjectId</c> when set, else default scope ids.
/// </remarks>
public sealed class AuthorityReplayService(
    IAuthorityQueryService queryService,
    IScopeContextProvider scopeContextProvider,
    IDecisionEngine decisionEngine,
    IArtifactSynthesisService artifactSynthesisService,
    IManifestHashService manifestHashService,
    IDecisionTraceRepository decisionTraceRepository,
    IGoldenManifestRepository goldenManifestRepository,
    IArtifactBundleRepository artifactBundleRepository)
    : IAuthorityReplayService
{
    /// <inheritdoc />
    public async Task<ReplayResult?> ReplayAsync(
        ReplayRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        ScopeContext readScope = scopeContextProvider.GetCurrentScope();
        RunDetailDto? original = await queryService.GetRunDetailAsync(readScope, request.RunId, ct);
        if (original is null)
            return null;

        string mode = string.IsNullOrWhiteSpace(request.Mode)
            ? ReplayMode.ReconstructOnly
            : request.Mode.Trim();

        ReplayResult result = new()
        {
            RunId = request.RunId,
            Mode = mode,
            ReplayedUtc = DateTime.UtcNow,
            Original = original,
            Validation = new ReplayValidationResult
            {
                ContextPresent = original.ContextSnapshot is not null,
                GraphPresent = original.GraphSnapshot is not null,
                FindingsPresent = original.FindingsSnapshot is not null,
                ManifestPresent = original.GoldenManifest is not null,
                TracePresent = original.DecisionTrace is not null,
                ArtifactsPresent = original.ArtifactBundle is not null
            }
        };

        if (original.GoldenManifest is not null)
        {
            string computedHash = manifestHashService.ComputeHash(original.GoldenManifest);
            result.Validation.ManifestHashMatches =
                string.Equals(computedHash, original.GoldenManifest.ManifestHash, StringComparison.OrdinalIgnoreCase);

            if (!result.Validation.ManifestHashMatches)
            
                result.Validation.Notes.Add("Stored manifest hash does not match recomputed manifest hash.");
            
        }
        else
        
            result.Validation.Notes.Add("No golden manifest on run; manifest hash validation skipped.");
        

        if (string.Equals(mode, ReplayMode.ReconstructOnly, StringComparison.OrdinalIgnoreCase))
        {
            result.Validation.Notes.Add("Replay completed in reconstruct-only mode.");
            return result;
        }

        if (original.ContextSnapshot is null || original.GraphSnapshot is null || original.FindingsSnapshot is null)
        {
            result.Validation.Notes.Add("Replay rebuild requested, but required authority records are missing.");
            return result;
        }

        (GoldenManifest manifest, RuleAuditTrace trace) = await decisionEngine.DecideAsync(
            original.Run.RunId,
            original.ContextSnapshot.SnapshotId,
            original.GraphSnapshot,
            original.FindingsSnapshot,
            ct);

        ScopeContext writeScope = WriteScopeFromRun(original.Run);
        ApplyScope(trace, writeScope);
        ApplyScope(manifest, writeScope);
        manifest.ManifestHash = manifestHashService.ComputeHash(manifest);

        await decisionTraceRepository.SaveAsync(trace, ct);
        await goldenManifestRepository.SaveAsync(manifest, ct);

        result.RebuiltManifest = manifest;

        string rebuiltHash = manifestHashService.ComputeHash(manifest);

        if (original.GoldenManifest is not null)
        
            if (string.Equals(rebuiltHash, original.GoldenManifest.ManifestHash, StringComparison.OrdinalIgnoreCase))
            
                result.Validation.Notes.Add("Rebuilt manifest hash matches original manifest hash.");
            
            else
            {
                result.Validation.Notes.Add("Rebuilt manifest hash differs from original manifest hash.");
                result.Validation.Notes.Add(
                    "Note: Rebuilt manifests receive new ManifestId/DecisionTraceId; hashes often differ even when rule logic matches.");
            }
        

        if (!string.Equals(mode, ReplayMode.RebuildArtifacts, StringComparison.OrdinalIgnoreCase))
            return result;

        ArtifactBundle rebuiltArtifacts = await artifactSynthesisService.SynthesizeAsync(
            manifest,
            ct);

        await artifactBundleRepository.SaveAsync(rebuiltArtifacts, ct);

        result.RebuiltArtifactBundle = rebuiltArtifacts;
        result.Validation.ArtifactBundlePresentAfterReplay = rebuiltArtifacts.Artifacts.Count > 0;

        if (original.ArtifactBundle is null)
            return result;

        result.Validation.Notes.Add(original.ArtifactBundle.Artifacts.Count == rebuiltArtifacts.Artifacts.Count
            ? "Rebuilt artifact count matches original artifact count."
            : "Rebuilt artifact count differs from original artifact count.");

        return result;
    }

    private static ScopeContext WriteScopeFromRun(RunRecord run)
    {
        if (run.TenantId == Guid.Empty && run.WorkspaceId == Guid.Empty && run.ScopeProjectId == Guid.Empty)
        
            return new ScopeContext
            {
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject
            };
        

        return new ScopeContext
        {
            TenantId = run.TenantId,
            WorkspaceId = run.WorkspaceId,
            ProjectId = run.ScopeProjectId
        };
    }

    private static void ApplyScope(RuleAuditTrace trace, ScopeContext scope)
    {
        trace.TenantId = scope.TenantId;
        trace.WorkspaceId = scope.WorkspaceId;
        trace.ProjectId = scope.ProjectId;
    }

    private static void ApplyScope(GoldenManifest manifest, ScopeContext scope)
    {
        manifest.TenantId = scope.TenantId;
        manifest.WorkspaceId = scope.WorkspaceId;
        manifest.ProjectId = scope.ProjectId;
    }
}
