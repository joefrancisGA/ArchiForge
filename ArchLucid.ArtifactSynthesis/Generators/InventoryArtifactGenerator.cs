using System.Text.Json;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Manifest.Sections;
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
        InventoryArtifactModel inventory = new();

        foreach (RequirementCoverageItem requirement in manifest.Requirements.Covered)
        
            inventory.Items.Add(new InventoryItem
            {
                Category = "Requirement",
                Name = requirement.RequirementName,
                Status = requirement.CoverageStatus,
                Notes = requirement.RequirementText
            });
        

        foreach (RequirementCoverageItem requirement in manifest.Requirements.Uncovered)
        
            inventory.Items.Add(new InventoryItem
            {
                Category = "Requirement",
                Name = requirement.RequirementName,
                Status = requirement.CoverageStatus,
                Notes = requirement.RequirementText
            });
        

        foreach (SecurityPostureItem control in manifest.Security.Controls)
        
            inventory.Items.Add(new InventoryItem
            {
                Category = "SecurityControl",
                Name = control.ControlName,
                Status = control.Status,
                Notes = control.Impact
            });
        

        foreach (CompliancePostureItem control in manifest.Compliance.Controls)
        
            inventory.Items.Add(new InventoryItem
            {
                Category = "ComplianceControl",
                Name = control.ControlName,
                Status = control.Status,
                Notes = control.AppliesToCategory
            });
        

        foreach (ManifestIssue issue in manifest.UnresolvedIssues.Items)
        
            inventory.Items.Add(new InventoryItem
            {
                Category = "Issue",
                Name = issue.Title,
                Status = issue.Severity,
                Notes = issue.Description
            });
        

        string content = JsonSerializer.Serialize(inventory, SynthesisJsonOptions.WriteIndented);

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
