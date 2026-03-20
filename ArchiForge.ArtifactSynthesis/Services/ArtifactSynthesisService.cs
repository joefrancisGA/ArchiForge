using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Services;

public class ArtifactSynthesisService : IArtifactSynthesisService
{
    private readonly IEnumerable<IArtifactGenerator> _generators;
    private readonly IArtifactBundleValidator _validator;

    public ArtifactSynthesisService(
        IEnumerable<IArtifactGenerator> generators,
        IArtifactBundleValidator validator)
    {
        _generators = generators;
        _validator = validator;
    }

    public async Task<ArtifactBundle> SynthesizeAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        var bundle = new ArtifactBundle
        {
            TenantId = manifest.TenantId,
            WorkspaceId = manifest.WorkspaceId,
            ProjectId = manifest.ProjectId,
            BundleId = Guid.NewGuid(),
            RunId = manifest.RunId,
            ManifestId = manifest.ManifestId,
            CreatedUtc = DateTime.UtcNow,
            Trace = new SynthesisTrace
            {
                TraceId = Guid.NewGuid(),
                RunId = manifest.RunId,
                ManifestId = manifest.ManifestId,
                CreatedUtc = DateTime.UtcNow,
                SourceDecisionIds = manifest.Decisions.Select(x => x.DecisionId).ToList()
            }
        };

        var decisionIds = manifest.Decisions.Select(x => x.DecisionId).ToList();

        foreach (var generator in _generators.OrderBy(x => x.ArtifactType, StringComparer.OrdinalIgnoreCase))
        {
            var artifact = await generator.GenerateAsync(manifest, ct);
            foreach (var id in decisionIds)
                artifact.ContributingDecisionIds.Add(id);
            bundle.Artifacts.Add(artifact);
            bundle.Trace.GeneratorsUsed.Add(generator.GetType().Name);
        }

        if (bundle.Artifacts.Count == 0)
        {
            bundle.Trace.Notes.Add("No artifacts were generated.");
        }

        _validator.Validate(bundle);

        return bundle;
    }
}
