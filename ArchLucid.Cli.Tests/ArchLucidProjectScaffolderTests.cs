using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArchLucidProjectScaffolderTests
{
    [Fact]
    public void CreateProject_with_null_options_throws_ArgumentNullException()
    {
        Action act = () => ArchLucidProjectScaffolder.CreateProject(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateProject_with_IncludeTerraformStubs_false_does_not_write_terraform_files()
    {
        using TempDirectory temp = new();
        string projectRoot = ArchLucidProjectScaffolder.CreateProject(
            new ArchLucidProjectScaffolder.ScaffoldOptions
            {
                ProjectName = "NoTf",
                BaseDirectory = temp.Path,
                OverwriteExistingFiles = true,
                IncludeTerraformStubs = false
            });

        File.Exists(Path.Combine(projectRoot, "infra", "terraform", "main.tf")).Should().BeFalse();
        File.Exists(Path.Combine(projectRoot, ArchLucidProjectScaffolder.CliManifestFileName)).Should().BeTrue();
    }

    [Fact]
    public void CreateProject_RegisterProject_without_connection_string_throws_InvalidOperationException()
    {
        using TempDirectory temp = new();

        Action act = () => ArchLucidProjectScaffolder.CreateProject(
            new ArchLucidProjectScaffolder.ScaffoldOptions
            {
                ProjectName = "Reg",
                BaseDirectory = temp.Path,
                OverwriteExistingFiles = true,
                RegisterProject = true
            });

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionString*");
    }

    [Fact]
    public void CreateProject_QuickStartEvaluation_writes_local_appsettings_sqlite_and_documentation()
    {
        using TempDirectory temp = new();
        string projectRoot = ArchLucidProjectScaffolder.CreateProject(
            new ArchLucidProjectScaffolder.ScaffoldOptions
            {
                ProjectName = "Quick",
                BaseDirectory = temp.Path,
                OverwriteExistingFiles = true,
                QuickStartEvaluation = true
            });

        File.Exists(Path.Combine(projectRoot, "local", "archlucid.quickstart.appsettings.json")).Should().BeTrue();
        File.Exists(Path.Combine(projectRoot, "local", "archlucid-evaluation.sqlite")).Should().BeTrue();
        string appsettings = File.ReadAllText(Path.Combine(projectRoot, "local", "archlucid.quickstart.appsettings.json"));
        appsettings.Should().Contain("\"StorageProvider\"").And.Contain("InMemory");

        string readme = File.ReadAllText(Path.Combine(projectRoot, "docs", "README.md"));

        readme.Should().Contain("--quickstart");
    }

    [Fact]
    public void LoadConfig_when_only_legacy_manifest_filename_exists_loads_same_as_archlucid_json()
    {
        using TempDirectory temp = new();
        string validJson = """
                           {
                             "schemaVersion": "1.0",
                             "projectName": "LegacyNamed",
                             "inputs": { "brief": "inputs/brief.md" },
                             "outputs": { "localCacheDir": "outputs" },
                             "plugins": { "lockFile": "plugins/plugin-lock.json" },
                             "infra": { "terraform": { "enabled": false, "path": "infra/terraform" } }
                           }
                           """;
        string legacyName = "archi" + "forge.json";
        File.WriteAllText(Path.Combine(temp.Path, legacyName), validJson);
        Directory.CreateDirectory(Path.Combine(temp.Path, "inputs"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins"));
        File.WriteAllText(Path.Combine(temp.Path, "inputs", "brief.md"), "# Brief");
        File.WriteAllText(Path.Combine(temp.Path, "plugins", "plugin-lock.json"), "{}");

        ArchLucidProjectScaffolder.ArchLucidCliConfig config = ArchLucidProjectScaffolder.LoadConfig(temp.Path);

        config.ProjectName.Should().Be("LegacyNamed");
    }

    [Fact]
    public void LoadConfig_when_schema_version_empty_throws_InvalidDataException()
    {
        using TempDirectory temp = new();
        string json = """
                      {
                        "schemaVersion": "",
                        "projectName": "P",
                        "inputs": { "brief": "inputs/brief.md" },
                        "outputs": { "localCacheDir": "outputs" },
                        "plugins": { "lockFile": "plugins/plugin-lock.json" },
                        "infra": { "terraform": { "enabled": false, "path": "infra/terraform" } }
                      }
                      """;
        File.WriteAllText(Path.Combine(temp.Path, ArchLucidProjectScaffolder.CliManifestFileName), json);
        Directory.CreateDirectory(Path.Combine(temp.Path, "inputs"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins"));
        File.WriteAllText(Path.Combine(temp.Path, "inputs", "brief.md"), "# Brief");
        File.WriteAllText(Path.Combine(temp.Path, "plugins", "plugin-lock.json"), "{}");

        Action act = () => ArchLucidProjectScaffolder.LoadConfig(temp.Path);

        act.Should().Throw<InvalidDataException>().WithMessage("*schemaVersion is required*");
    }

    [Fact]
    public void LoadConfig_when_inputs_brief_is_rooted_throws_InvalidDataException()
    {
        using TempDirectory temp = new();
        string rootedBrief = Path.Combine(temp.Path, "abs-brief.md");
        object payload = new
        {
            schemaVersion = "1.0",
            projectName = "P",
            inputs = new { brief = rootedBrief },
            outputs = new { localCacheDir = "outputs" },
            plugins = new { lockFile = "plugins/plugin-lock.json" },
            infra = new { terraform = new { enabled = false, path = "infra/terraform" } }
        };
        string json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
        File.WriteAllText(Path.Combine(temp.Path, ArchLucidProjectScaffolder.CliManifestFileName), json);
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins"));
        File.WriteAllText(Path.Combine(temp.Path, "plugins", "plugin-lock.json"), "{}");

        Action act = () => ArchLucidProjectScaffolder.LoadConfig(temp.Path);

        act.Should().Throw<InvalidDataException>().WithMessage("*relative path*");
    }

    [Fact]
    public void LoadConfig_when_terraform_enabled_and_directory_missing_throws_DirectoryNotFoundException()
    {
        using TempDirectory temp = new();
        string json = """
                      {
                        "schemaVersion": "1.0",
                        "projectName": "P",
                        "inputs": { "brief": "inputs/brief.md" },
                        "outputs": { "localCacheDir": "outputs" },
                        "plugins": { "lockFile": "plugins/plugin-lock.json" },
                        "infra": { "terraform": { "enabled": true, "path": "infra/terraform" } }
                      }
                      """;
        File.WriteAllText(Path.Combine(temp.Path, ArchLucidProjectScaffolder.CliManifestFileName), json);
        Directory.CreateDirectory(Path.Combine(temp.Path, "inputs"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins"));
        File.WriteAllText(Path.Combine(temp.Path, "inputs", "brief.md"), "# Brief");
        File.WriteAllText(Path.Combine(temp.Path, "plugins", "plugin-lock.json"), "{}");

        Action act = () => ArchLucidProjectScaffolder.LoadConfig(temp.Path);

        act.Should().Throw<DirectoryNotFoundException>();
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
        } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "ArchLucid.Cli.Tests." + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
