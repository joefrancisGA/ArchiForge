using FluentAssertions;

namespace ArchiForge.Cli.Tests;

public sealed class ArchiForgeConfigTests
{
    [Fact]
    public void LoadConfig_ValidJsonAndFilesExist_ReturnsConfig()
    {
        using TempDirectory temp = new();
        string validJson = """
                           {
                             "schemaVersion": "1.0",
                             "projectName": "TestProject",
                             "inputs": { "brief": "inputs/brief.md" },
                             "outputs": { "localCacheDir": "outputs" },
                             "plugins": { "lockFile": "plugins/plugin-lock.json" },
                             "infra": { "terraform": { "enabled": false, "path": "infra/terraform" } }
                           }
                           """;
        File.WriteAllText(Path.Combine(temp.Path, "archiforge.json"), validJson);
        Directory.CreateDirectory(Path.Combine(temp.Path, "inputs"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins"));
        File.WriteAllText(Path.Combine(temp.Path, "inputs", "brief.md"), "# Brief");
        File.WriteAllText(Path.Combine(temp.Path, "plugins", "plugin-lock.json"), "{}");

        ArchiForgeProjectScaffolder.ArchiForgeConfig config = ArchiForgeProjectScaffolder.LoadConfig(temp.Path);

        config.Should().NotBeNull();
        config.ProjectName.Should().Be("TestProject");
        config.SchemaVersion.Should().Be("1.0");
        config.Inputs.Should().NotBeNull();
        config.Inputs.Brief.Should().Be("inputs/brief.md");
        config.Outputs.LocalCacheDir.Should().Be("outputs");
        config.Plugins.LockFile.Should().Be("plugins/plugin-lock.json");
        config.Infra.Terraform.Should().NotBeNull();
    }

    [Fact]
    public void LoadConfig_MissingManifestFile_ThrowsFileNotFoundException()
    {
        using TempDirectory temp = new();

        Func<ArchiForgeProjectScaffolder.ArchiForgeConfig> act = () => ArchiForgeProjectScaffolder.LoadConfig(temp.Path);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*archiforge.json*");
    }

    [Fact]
    public void LoadConfig_InvalidJson_ThrowsInvalidDataException()
    {
        using TempDirectory temp = new();
        File.WriteAllText(Path.Combine(temp.Path, "archiforge.json"), "{ invalid json }");

        Func<ArchiForgeProjectScaffolder.ArchiForgeConfig> act = () => ArchiForgeProjectScaffolder.LoadConfig(temp.Path);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void LoadConfig_MissingBriefFile_Throws()
    {
        using TempDirectory temp = new();
        string validJson = """
                           {
                             "schemaVersion": "1.0",
                             "projectName": "TestProject",
                             "inputs": { "brief": "inputs/brief.md" },
                             "outputs": { "localCacheDir": "outputs" },
                             "plugins": { "lockFile": "plugins/plugin-lock.json" },
                             "infra": { "terraform": { "enabled": false, "path": "infra/terraform" } }
                           }
                           """;
        File.WriteAllText(Path.Combine(temp.Path, "archiforge.json"), validJson);
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins"));
        File.WriteAllText(Path.Combine(temp.Path, "plugins", "plugin-lock.json"), "{}");
        // Do not create inputs/brief.md

        Func<ArchiForgeProjectScaffolder.ArchiForgeConfig> act = () => ArchiForgeProjectScaffolder.LoadConfig(temp.Path);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Brief*");
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ArchiForge.Cli.Tests." + Guid.NewGuid().ToString("N")[..8]);

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
