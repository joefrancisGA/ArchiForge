using System.IO.Compression;
using System.Text;

using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ArtifactSynthesis.Packaging;

using FluentAssertions;

namespace ArchLucid.ArtifactSynthesis.Tests;

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
            new()
            {
                Name = "a.txt",
                Content = "a",
                Format = "text",
                ContentHash = "ha",
                ArtifactType = "A",
                ArtifactId = Guid.NewGuid(),
            },
            new()
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

    [Fact]
    public void BuildRunExportPackage_readme_includes_optional_manifest_context()
    {
        ArtifactPackagingService sut = new(new FixedContentTypeResolver());
        Guid runId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();
        const string manifestJson = """{"manifestId":"00000000-0000-0000-0000-000000000001"}""";
        RunExportReadmeContext readme = new()
        {
            ManifestDisplayName = "Staging rollout",
            ManifestHash = "sha-example",
            RuleSetLabel = "default-rules 2.0",
        };

        ArtifactPackage package = sut.BuildRunExportPackage(
            runId,
            manifestId,
            [],
            manifestJson,
            traceJson: null,
            readme);

        using MemoryStream stream = new(package.Content);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        ZipArchiveEntry? readmeEntry = archive.GetEntry("README.txt");
        readmeEntry.Should().NotBeNull();
        using StreamReader reader = new(readmeEntry!.Open(), Encoding.UTF8);
        string text = reader.ReadToEnd();
        text.Should().Contain(runId.ToString());
        text.Should().Contain(manifestId.ToString());
        text.Should().Contain("Staging rollout");
        text.Should().Contain("sha-example");
        text.Should().Contain("default-rules 2.0");
        text.Should().Contain("manifest.json");
    }
}
