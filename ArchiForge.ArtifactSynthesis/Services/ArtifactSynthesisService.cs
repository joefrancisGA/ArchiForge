using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;

using Microsoft.Extensions.Logging;

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
    IArtifactBundleValidator validator,
    ILogger<ArtifactSynthesisService> logger)
    : IArtifactSynthesisService
{
    private const string NoArtifactsNote = "No artifacts were generated.";
    public async Task<ArtifactBundle> SynthesizeAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Artifact synthesis starting: RunId={RunId}, ManifestId={ManifestId}, GeneratorCount={GeneratorCount}",
                manifest.RunId,
                manifest.ManifestId,
                generators.Count());
        }

        ArtifactBundle bundle = new()
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
        {
            bundle.Trace.Notes.Add(NoArtifactsNote);

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Artifact synthesis produced zero artifacts: RunId={RunId}, ManifestId={ManifestId}, TraceId={TraceId}",
                    manifest.RunId,
                    manifest.ManifestId,
                    bundle.Trace.TraceId);
            }
        }

        validator.Validate(bundle);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Artifact synthesis completed: RunId={RunId}, ManifestId={ManifestId}, TraceId={TraceId}, ArtifactCount={ArtifactCount}, GeneratorsUsed={GeneratorsUsed}",
                manifest.RunId,
                manifest.ManifestId,
                bundle.Trace.TraceId,
                bundle.Artifacts.Count,
                string.Join(',', bundle.Trace.GeneratorsUsed));
        }

        return bundle;
    }
}
