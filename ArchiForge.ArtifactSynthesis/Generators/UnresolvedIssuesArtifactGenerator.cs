using System.Text.Json;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Generators;

public class UnresolvedIssuesArtifactGenerator : IArtifactGenerator
{
    public string ArtifactType => global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.UnresolvedIssuesReport;

    public Task<SynthesizedArtifact> GenerateAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        _ = ct;
        UnresolvedIssuesArtifactModel model = new UnresolvedIssuesArtifactModel
        {
            Items = manifest.UnresolvedIssues.Items.Select(x => new UnresolvedIssueArtifactItem
            {
                IssueType = x.IssueType,
                Title = x.Title,
                Description = x.Description,
                Severity = x.Severity
            }).ToList()
        };

        string content = JsonSerializer.Serialize(model, SynthesisJsonOptions.WriteIndented);

        return Task.FromResult(new SynthesizedArtifact
        {
            ArtifactId = Guid.NewGuid(),
            RunId = manifest.RunId,
            ManifestId = manifest.ManifestId,
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.UnresolvedIssuesReport,
            Name = "unresolved-issues.json",
            Format = "json",
            Content = content,
            ContentHash = ArtifactHashing.ComputeHash(content)
        });
    }
}
