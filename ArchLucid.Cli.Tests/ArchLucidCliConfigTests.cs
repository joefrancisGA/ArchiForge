using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
///     Tests for Archi Forge Config.
/// </summary>
[Trait("Suite", "Core")]
public sealed class ArchLucidConfigTests
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
        File.WriteAllText(Path.Combine(temp.Path, ArchLucidProjectScaffolder.CliManifestFileName), validJson);
        Directory.CreateDirectory(Path.Combine(temp.Path, "inputs"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins"));
        File.WriteAllText(Path.Combine(temp.Path, "inputs", "brief.md"), "# Brief");
        File.WriteAllText(Path.Combine(temp.Path, "plugins", "plugin-lock.json"), "{}");

        ArchLucidProjectScaffolder.ArchLucidCliConfig config = ArchLucidProjectScaffolder.LoadConfig(temp.Path);

        config.Should().NotBeNull();
        config.ProjectName.Should().Be("TestProject");
        config.SchemaVersion.Should().Be("1.0");
        config.Inputs.Should().NotBeNull();
        config.Inputs.Brief.Should().Be("inputs/brief.md");
        config.Outputs.LocalCacheDir.Should().Be("outputs");
        config.Plugins!.LockFile.Should().Be("plugins/plugin-lock.json");
        config.Infra!.Terraform.Should().NotBeNull();
    }

    [Fact]
    public void LoadConfig_when_plugins_and_infra_omitted_validates_brief_and_outputs_only()
    {
        using TempDirectory temp = new();
        string minimalJson = """
                             {
                               "schemaVersion": "1.0",
                               "projectName": "Minimal",
                               "inputs": { "brief": "inputs/brief.md" },
                               "outputs": { "localCacheDir": "outputs" }
                             }
                             """;
        File.WriteAllText(Path.Combine(temp.Path, ArchLucidProjectScaffolder.CliManifestFileName), minimalJson);
        Directory.CreateDirectory(Path.Combine(temp.Path, "inputs"));
        File.WriteAllText(Path.Combine(temp.Path, "inputs", "brief.md"),
            "# Brief long enough for validation elsewhere");

        ArchLucidProjectScaffolder.ArchLucidCliConfig config = ArchLucidProjectScaffolder.LoadConfig(temp.Path);

        config.Plugins.Should().BeNull();
        config.Infra.Should().BeNull();
        config.ProjectName.Should().Be("Minimal");
    }

    [Fact]
    public void LoadConfig_MissingManifestFile_ThrowsFileNotFoundException()
    {
        using TempDirectory temp = new();

        Func<ArchLucidProjectScaffolder.ArchLucidCliConfig>
            act = () => ArchLucidProjectScaffolder.LoadConfig(temp.Path);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*" + ArchLucidProjectScaffolder.CliManifestFileName + "*");
    }

    [Fact]
    public void LoadConfig_InvalidJson_ThrowsInvalidDataException()
    {
        using TempDirectory temp = new();
        File.WriteAllText(Path.Combine(temp.Path, ArchLucidProjectScaffolder.CliManifestFileName), "{ invalid json }");

        Func<ArchLucidProjectScaffolder.ArchLucidCliConfig>
            act = () => ArchLucidProjectScaffolder.LoadConfig(temp.Path);

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
        File.WriteAllText(Path.Combine(temp.Path, ArchLucidProjectScaffolder.CliManifestFileName), validJson);
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins"));
        File.WriteAllText(Path.Combine(temp.Path, "plugins", "plugin-lock.json"), "{}");
        // Do not create inputs/brief.md

        Func<ArchLucidProjectScaffolder.ArchLucidCliConfig>
            act = () => ArchLucidProjectScaffolder.LoadConfig(temp.Path);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Brief*");
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public string Path
        {
            get;
        } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "ArchLucid.Cli.Tests." + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
