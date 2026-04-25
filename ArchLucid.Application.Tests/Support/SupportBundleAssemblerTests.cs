using System.IO.Compression;
using System.Text;

using ArchLucid.Application.Support;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Support;

[Trait("Category", "Unit")]
public sealed class SupportBundleAssemblerTests
{
    [Fact]
    public async Task AssembleAsync_NullRequest_Throws()
    {
        SupportBundleAssembler assembler = new(TimeProvider.System);

        Func<Task> act = () => assembler.AssembleAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AssembleAsync_ContainsCanonicalEntryNames()
    {
        SupportBundleAssembler assembler = new(new FakeTimeProvider(new DateTimeOffset(2026, 4, 24, 10, 15, 30, TimeSpan.Zero)));

        SupportBundleArtifact artifact = await assembler.AssembleAsync(new SupportBundleRequest("op@example.com", "Acme"));

        IReadOnlyList<string> entryNames = ListEntryNames(artifact.Bytes);
        entryNames.Should().Contain(SupportBundleAssembler.ReadmeFileName);
        entryNames.Should().Contain(SupportBundleAssembler.ManifestFileName);
        entryNames.Should().Contain(SupportBundleAssembler.BuildFileName);
        entryNames.Should().Contain(SupportBundleAssembler.EnvironmentFileName);
        entryNames.Should().Contain(SupportBundleAssembler.ReferencesFileName);
    }

    [Fact]
    public async Task AssembleAsync_FileNameUsesGeneratedTimestamp()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 24, 10, 15, 30, TimeSpan.Zero));
        SupportBundleAssembler assembler = new(time);

        SupportBundleArtifact artifact = await assembler.AssembleAsync(new SupportBundleRequest("op@example.com", "Acme"));

        artifact.FileName.Should().Be("archlucid-support-bundle-20260424-101530Z.zip");
        artifact.ContentType.Should().Be("application/zip");
        artifact.GeneratedUtc.Should().Be(time.GetUtcNow());
    }

    [Fact]
    public async Task AssembleAsync_ReadmeIncludesRequesterAndTenantDisplay()
    {
        SupportBundleAssembler assembler = new(TimeProvider.System);

        SupportBundleArtifact artifact = await assembler.AssembleAsync(
            new SupportBundleRequest("alice@example.com", "Acme Inc"));

        string readme = ReadEntryAsText(artifact.Bytes, SupportBundleAssembler.ReadmeFileName);

        readme.Should().Contain("alice@example.com");
        readme.Should().Contain("Acme Inc");
    }

    [Fact]
    public async Task AssembleAsync_NullRequesterAndTenant_FallBackToPlaceholders()
    {
        SupportBundleAssembler assembler = new(TimeProvider.System);

        SupportBundleArtifact artifact = await assembler.AssembleAsync(new SupportBundleRequest(null, null));

        string readme = ReadEntryAsText(artifact.Bytes, SupportBundleAssembler.ReadmeFileName);

        readme.Should().Contain("(unknown operator)");
        readme.Should().Contain("(no tenant context)");
    }

    [Fact]
    public async Task AssembleAsync_ManifestDeclaresServerSourceAndBundleFormatVersion()
    {
        SupportBundleAssembler assembler = new(TimeProvider.System);

        SupportBundleArtifact artifact = await assembler.AssembleAsync(new SupportBundleRequest("op", "tenant"));

        string manifest = ReadEntryAsText(artifact.Bytes, SupportBundleAssembler.ManifestFileName);

        manifest.Should().Contain("\"source\": \"api\"");
        manifest.Should().Contain($"\"bundleFormatVersion\": \"{SupportBundleAssembler.BundleFormatVersion}\"");
    }

    [Fact]
    public async Task AssembleAsync_CancellationTokenAlreadyCancelled_ThrowsOperationCanceled()
    {
        SupportBundleAssembler assembler = new(TimeProvider.System);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        Func<Task> act = () => assembler.AssembleAsync(new SupportBundleRequest("op", null), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static IReadOnlyList<string> ListEntryNames(byte[] zipBytes)
    {
        using MemoryStream memory = new(zipBytes);
        using ZipArchive archive = new(memory, ZipArchiveMode.Read);
        return [.. archive.Entries.Select(e => e.FullName)];
    }

    private static string ReadEntryAsText(byte[] zipBytes, string entryName)
    {
        using MemoryStream memory = new(zipBytes);
        using ZipArchive archive = new(memory, ZipArchiveMode.Read);
        ZipArchiveEntry? entry = archive.GetEntry(entryName);

        if (entry is null) throw new InvalidOperationException($"Entry '{entryName}' missing from bundle.");

        using Stream stream = entry.Open();
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
