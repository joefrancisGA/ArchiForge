using System.Text.Json;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Generators;

public class InventoryArtifactGenerator : IArtifactGenerator
{
    public string ArtifactType => global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.Inventory;

    public Task<SynthesizedArtifact> GenerateAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        _ = ct;
        var inventory = new InventoryArtifactModel();

        foreach (var requirement in manifest.Requirements.Covered)
        {
            inventory.Items.Add(new InventoryItem
            {
                Category = "Requirement",
                Name = requirement.RequirementName,
                Status = requirement.CoverageStatus,
                Notes = requirement.RequirementText
            });
        }

        foreach (var control in manifest.Security.Controls)
        {
            inventory.Items.Add(new InventoryItem
            {
                Category = "SecurityControl",
                Name = control.ControlName,
                Status = control.Status,
                Notes = control.Impact
            });
        }

        foreach (var issue in manifest.UnresolvedIssues.Items)
        {
            inventory.Items.Add(new InventoryItem
            {
                Category = "Issue",
                Name = issue.Title,
                Status = issue.Severity,
                Notes = issue.Description
            });
        }

        var content = JsonSerializer.Serialize(inventory, SynthesisJsonOptions.WriteIndented);

        return Task.FromResult(new SynthesizedArtifact
        {
            ArtifactId = Guid.NewGuid(),
            RunId = manifest.RunId,
            ManifestId = manifest.ManifestId,
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.Inventory,
            Name = "inventory.json",
            Format = "json",
            Content = content,
            ContentHash = ArtifactHashing.ComputeHash(content)
        });
    }
}
