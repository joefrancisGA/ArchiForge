using FluentAssertions;

using System.IO.Compression;
using System.Text;

using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Packaging;

namespace ArchiForge.ArtifactSynthesis.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArtifactPackagingServiceTests
{
    private sealed class FixedContentTypeResolver : IArtifactContentTypeResolver
    {
        public string Resolve(SynthesizedArtifact artifact)
        {
            _ = artifact;

            return "text/plain";
        }
    }

    [Fact]
    public void BuildSingleFileExport_sanitizes_name_and_encodes_utf8()
    {
        ArtifactPackagingService sut = new(new FixedContentTypeResolver());
        SynthesizedArtifact artifact = new()
        {
            Name = "bad|name?.txt",
            Content = "hello",
            Format = "text",
            ContentHash = "x",
            ArtifactType = "t",
        };

        ArtifactFileExport export = sut.BuildSingleFileExport(artifact);

        export.FileName.Should().NotContain("|").And.NotContain("?");
        Encoding.UTF8.GetString(export.Content).Should().Be("hello");
        export.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void BuildBundlePackage_writes_zip_with_index_and_metadata()
    {
        ArtifactPackagingService sut = new(new FixedContentTypeResolver());
        Guid manifestId = Guid.NewGuid();
        List<SynthesizedArtifact> artifacts =
        [
            new SynthesizedArtifact
            {
                Name = "a.txt",
                Content = "a",
                Format = "text",
                ContentHash = "ha",
                ArtifactType = "A",
                ArtifactId = Guid.NewGuid(),
            },
            new SynthesizedArtifact
            {
                Name = "b.txt",
                Content = "b",
                Format = "text",
                ContentHash = "hb",
                ArtifactType = "B",
                ArtifactId = Guid.NewGuid(),
            },
        ];

        ArtifactPackage package = sut.BuildBundlePackage(manifestId, artifacts);

        package.PackageFileName.Should().Contain(manifestId.ToString("N"));
        using MemoryStream stream = new(package.Content);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        archive.GetEntry("bundle-index.json").Should().NotBeNull();
        archive.GetEntry("package-metadata.json").Should().NotBeNull();
        archive.GetEntry("a.txt").Should().NotBeNull();
        archive.GetEntry("b.txt").Should().NotBeNull();
    }
}
