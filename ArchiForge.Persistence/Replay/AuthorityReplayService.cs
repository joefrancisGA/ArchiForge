using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Persistence.Queries;

namespace ArchiForge.Persistence.Replay;

public sealed class AuthorityReplayService : IAuthorityReplayService
{
    private readonly IAuthorityQueryService _queryService;
    private readonly IDecisionEngine _decisionEngine;
    private readonly IArtifactSynthesisService _artifactSynthesisService;
    private readonly IManifestHashService _manifestHashService;
    private readonly IDecisionTraceRepository _decisionTraceRepository;
    private readonly IGoldenManifestRepository _goldenManifestRepository;
    private readonly IArtifactBundleRepository _artifactBundleRepository;

    public AuthorityReplayService(
        IAuthorityQueryService queryService,
        IDecisionEngine decisionEngine,
        IArtifactSynthesisService artifactSynthesisService,
        IManifestHashService manifestHashService,
        IDecisionTraceRepository decisionTraceRepository,
        IGoldenManifestRepository goldenManifestRepository,
        IArtifactBundleRepository artifactBundleRepository)
    {
        _queryService = queryService;
        _decisionEngine = decisionEngine;
        _artifactSynthesisService = artifactSynthesisService;
        _manifestHashService = manifestHashService;
        _decisionTraceRepository = decisionTraceRepository;
        _goldenManifestRepository = goldenManifestRepository;
        _artifactBundleRepository = artifactBundleRepository;
    }

    public async Task<ReplayResult?> ReplayAsync(
        ReplayRequest request,
        CancellationToken ct)
    {
        var original = await _queryService.GetRunDetailAsync(request.RunId, ct);
        if (original is null)
            return null;

        var mode = string.IsNullOrWhiteSpace(request.Mode)
            ? ReplayMode.ReconstructOnly
            : request.Mode.Trim();

        var result = new ReplayResult
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
            var computedHash = _manifestHashService.ComputeHash(original.GoldenManifest);
            result.Validation.ManifestHashMatches =
                string.Equals(computedHash, original.GoldenManifest.ManifestHash, StringComparison.OrdinalIgnoreCase);

            if (!result.Validation.ManifestHashMatches)
            {
                result.Validation.Notes.Add("Stored manifest hash does not match recomputed manifest hash.");
            }
        }
        else
        {
            result.Validation.Notes.Add("No golden manifest on run; manifest hash validation skipped.");
        }

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

        var decisionResult = await _decisionEngine.DecideAsync(
            original.Run.RunId,
            original.ContextSnapshot.SnapshotId,
            original.GraphSnapshot,
            original.FindingsSnapshot,
            ct);

        await _decisionTraceRepository.SaveAsync(decisionResult.Trace, ct);
        await _goldenManifestRepository.SaveAsync(decisionResult.Manifest, ct);

        result.RebuiltManifest = decisionResult.Manifest;

        var rebuiltHash = _manifestHashService.ComputeHash(decisionResult.Manifest);

        if (original.GoldenManifest is not null)
        {
            if (string.Equals(rebuiltHash, original.GoldenManifest.ManifestHash, StringComparison.OrdinalIgnoreCase))
            {
                result.Validation.Notes.Add("Rebuilt manifest hash matches original manifest hash.");
            }
            else
            {
                result.Validation.Notes.Add("Rebuilt manifest hash differs from original manifest hash.");
                result.Validation.Notes.Add(
                    "Note: Rebuilt manifests receive new ManifestId/DecisionTraceId; hashes often differ even when rule logic matches.");
            }
        }

        if (string.Equals(mode, ReplayMode.RebuildArtifacts, StringComparison.OrdinalIgnoreCase))
        {
            var rebuiltArtifacts = await _artifactSynthesisService.SynthesizeAsync(
                decisionResult.Manifest,
                ct);

            await _artifactBundleRepository.SaveAsync(rebuiltArtifacts, ct);

            result.RebuiltArtifactBundle = rebuiltArtifacts;
            result.Validation.ArtifactBundlePresentAfterReplay = rebuiltArtifacts.Artifacts.Count > 0;

            if (original.ArtifactBundle is not null)
            {
                if (original.ArtifactBundle.Artifacts.Count == rebuiltArtifacts.Artifacts.Count)
                {
                    result.Validation.Notes.Add("Rebuilt artifact count matches original artifact count.");
                }
                else
                {
                    result.Validation.Notes.Add("Rebuilt artifact count differs from original artifact count.");
                }
            }
        }

        return result;
    }
}
