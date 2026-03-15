using ArchiForge.Application.Diffs;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Api.Models;

public sealed class ManifestCompareResponse
{
    public GoldenManifest LeftManifest { get; set; } = new();

    public GoldenManifest RightManifest { get; set; } = new();

    public ManifestDiffResult Diff { get; set; } = new();
}
