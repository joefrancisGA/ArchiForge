using System.Text.Json;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Generators;

public class ComplianceMatrixArtifactGenerator : IArtifactGenerator
{
    public string ArtifactType => global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.ComplianceMatrix;

    public Task<SynthesizedArtifact> GenerateAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        _ = ct;
        ComplianceMatrixArtifactModel matrix = new ComplianceMatrixArtifactModel();

        foreach (CompliancePostureItem control in manifest.Compliance.Controls)
        {
            List<string> notes = manifest.Compliance.Gaps
                .Where(x => x.Contains(control.ControlName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            matrix.Rows.Add(new ComplianceMatrixRow
            {
                ControlId = control.ControlId,
                ControlName = control.ControlName,
                AppliesToCategory = control.AppliesToCategory,
                Status = control.Status,
                Notes = notes.Count == 0 ? string.Empty : string.Join(" | ", notes)
            });
        }

        string content = JsonSerializer.Serialize(matrix, SynthesisJsonOptions.WriteIndented);

        return Task.FromResult(new SynthesizedArtifact
        {
            ArtifactId = Guid.NewGuid(),
            RunId = manifest.RunId,
            ManifestId = manifest.ManifestId,
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.ComplianceMatrix,
            Name = "compliance-matrix.json",
            Format = "json",
            Content = content,
            ContentHash = ArtifactHashing.ComputeHash(content)
        });
    }
}
