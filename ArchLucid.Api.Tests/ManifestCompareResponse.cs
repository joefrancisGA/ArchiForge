using ArchLucid.Api.Tests.TestDtos;

namespace ArchLucid.Api.Tests;

public sealed class ManifestCompareResponse
{
    public ManifestDto LeftManifest
    {
        get;
        set;
    } = new();

    public ManifestDto RightManifest
    {
        get;
        set;
    } = new();

    public ManifestDiffDto Diff
    {
        get;
        set;
    } = new();
}
