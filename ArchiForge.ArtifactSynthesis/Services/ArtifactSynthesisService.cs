using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Services;

/// <summary>
/// Synthesizes an <see cref="ArtifactBundle"/> from a committed <see cref="GoldenManifest"/> by invoking
/// all registered <see cref="IArtifactGenerator"/> implementations and validating the resulting bundle.
/// </summary>
/// <remarks>
/// Generators are invoked in ascending <see cref="IArtifactGenerator.ArtifactType"/> order to produce
/// deterministic bundle output. The bundle trace records which generators ran and any diagnostic notes.
/// </remarks>
public class ArtifactSynthesisService(
    IEnumerable<IArtifactGenerator> generators,
    IArtifactBundleValidator validator)
    : IArtifactSynthesisService
{
    private const string NoArtifactsNote = "No artifacts were generated.";
    public async Task<ArtifactBundle> SynthesizeAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArtifactBundle bundle = new ArtifactBundle
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

        List<string> decisionIds = manifest.Decisions.Select(x => x.DecisionId).ToList();

        foreach (IArtifactGenerator generator in generators.OrderBy(x => x.ArtifactType, StringComparer.OrdinalIgnoreCase))
        {
            SynthesizedArtifact artifact = await generator.GenerateAsync(manifest, ct);
            foreach (string id in decisionIds)
                artifact.ContributingDecisionIds.Add(id);
            bundle.Artifacts.Add(artifact);
            bundle.Trace.GeneratorsUsed.Add(generator.GetType().Name);
        }

        if (bundle.Artifacts.Count == 0)
            bundle.Trace.Notes.Add(NoArtifactsNote);

        validator.Validate(bundle);

        return bundle;
    }
}
