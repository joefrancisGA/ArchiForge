using ArchiForge.Api.Tests.TestDtos;

namespace ArchiForge.Api.Tests;

public sealed class ManifestCompareResponse
{
    public ManifestDto LeftManifest { get; set; } = new();
    public ManifestDto RightManifest { get; set; } = new();
    public ManifestDiffDto Diff { get; set; } = new();
}
