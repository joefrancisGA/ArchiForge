using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Data.SqlClient;


/*________________________________________
2) Folder layout created by archiforge new
    <projectName>/
archiforge.json
    inputs/
brief.md
    outputs/
.gitkeep
    plugins/
plugin-lock.json
    infra/
terraform/
main.tf
    variables.tf
docs/
README.md
    What each file means
•	archiforge.json: the single source of truth for project configuration.
•	inputs/brief.md: the "one thing you can always run."
•	outputs/: optional local cache of output artifacts (not authoritative).
    •	plugins/plugin-lock.json: pinned plugin images + versions + endpoints.
•	infra/terraform/: optional; stubbed initially.
*/



namespace ArchiForge.Cli;



public static class ArchiForgeProjectScaffolder
{
    /// <summary>Shared options for archiforge.json read/write (CA1869: single cached instance).</summary>
    private static readonly JsonSerializerOptions SJsonManifest = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public sealed class ScaffoldOptions
    {
        public string ProjectName { get; set; } = "";
        public string? BaseDirectory { get; set; } = null; // default: current directory
        public bool OverwriteExistingFiles { get; set; } = false;
        public bool IncludeTerraformStubs { get; set; } = true; // "optional; you can stub it initially"
        /// <summary>When true, attempt to register the project in SQL Server (PROJECTS table). Default false so scaffolding works without a database.</summary>
        public bool RegisterProject { get; set; } = false;
    }

    public static string CreateProject(ScaffoldOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ProjectName))
            throw new ArgumentException("ProjectName is required. It cannot be null, empty, or whitespace.", nameof(options));

        Console.WriteLine("Creating Project " + options.ProjectName);

        string baseDir = string.IsNullOrWhiteSpace(options.BaseDirectory)
            ? Directory.GetCurrentDirectory()
            : options.BaseDirectory!;

        string projectRoot = Path.Combine(baseDir, options.ProjectName);

        // Create directories
        CreateDirectory(projectRoot);
        CreateDirectory(Path.Combine(projectRoot, "inputs"));
        CreateDirectory(Path.Combine(projectRoot, "outputs"));
        CreateDirectory(Path.Combine(projectRoot, "plugins"));
        CreateDirectory(Path.Combine(projectRoot, "infra", "terraform"));
        CreateDirectory(Path.Combine(projectRoot, "docs"));

        // Write files
        WriteFile(Path.Combine(projectRoot, "archiforge.json"), BuildArchiForgeJson(options.ProjectName), options.OverwriteExistingFiles);
        WriteFile(Path.Combine(projectRoot, "inputs", "brief.md"), BuildBriefMd(options.ProjectName), options.OverwriteExistingFiles);
        WriteFile(Path.Combine(projectRoot, "outputs", ".gitkeep"), "", options.OverwriteExistingFiles);
        WriteFile(Path.Combine(projectRoot, "plugins", "plugin-lock.json"), BuildPluginLockJson(), options.OverwriteExistingFiles);

        if (options.IncludeTerraformStubs)
        {
            WriteFile(Path.Combine(projectRoot, "infra", "terraform", "main.tf"), BuildTerraformMainTf(), options.OverwriteExistingFiles);
            WriteFile(Path.Combine(projectRoot, "infra", "terraform", "variables.tf"), BuildTerraformVariablesTf(), options.OverwriteExistingFiles);
        }

        WriteFile(Path.Combine(projectRoot, "docs", "README.md"), BuildDocsReadme(options.ProjectName), options.OverwriteExistingFiles);

        if (options.RegisterProject)
        {
            const string connectionString = "Server=LOCALHOST;Database=ArchiForge;Trusted_Connection=True;";
            string sqlQuery = "INSERT INTO PROJECTS (ProjectName, BaseDirectory, OverwriteExistingFiles, IncludeTerraformStubs) VALUES (@ProjectName, @BaseDirectory, @OverwriteExistingFiles, @IncludeTerraformStubs)";

            try
            {
                using SqlConnection connection = new(connectionString);
                connection.Open();
                Console.WriteLine("Connection successful.");
                using SqlCommand command = new(sqlQuery, connection);
                command.Parameters.Add("@ProjectName", SqlDbType.NVarChar, 0).Value = options.ProjectName;
                command.Parameters.Add("@BaseDirectory", SqlDbType.NVarChar, 0).Value = options.BaseDirectory ?? (object)DBNull.Value;
                command.Parameters.Add("@OverwriteExistingFiles", SqlDbType.Bit, 0).Value = options.OverwriteExistingFiles;
                command.Parameters.Add("@IncludeTerraformStubs", SqlDbType.Bit, 0).Value = options.IncludeTerraformStubs;
                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Error: " + ex.Message);
            }
        }

        Console.WriteLine("Created Project " + options.ProjectName);
        return projectRoot;
    }

    private static void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    private static void WriteFile(string path, string contents, bool overwrite)
    {
        if (File.Exists(path) && !overwrite)
            return;
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public sealed class ArchiForgeConfig
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = "1.0";

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = "";

        [JsonPropertyName("apiUrl")]
        public string? ApiUrl
        {
            get; set;
        }

        [JsonPropertyName("inputs")]
        public InputsSection Inputs { get; set; } = new();

        [JsonPropertyName("outputs")]
        public OutputsSection Outputs { get; set; } = new();

        [JsonPropertyName("plugins")]
        public PluginsSection Plugins { get; set; } = new();

        [JsonPropertyName("infra")]
        public InfraSection Infra { get; set; } = new();

        [JsonPropertyName("architecture")]
        public ArchitectureSection? Architecture
        {
            get; set;
        }
    }

    public sealed class ArchitectureSection
    {
        [JsonPropertyName("environment")]
        public string? Environment
        {
            get; set;
        }

        [JsonPropertyName("cloudProvider")]
        public string? CloudProvider
        {
            get; set;
        }

        [JsonPropertyName("constraints")]
        public List<string>? Constraints
        {
            get; set;
        }

        [JsonPropertyName("requiredCapabilities")]
        public List<string>? RequiredCapabilities
        {
            get; set;
        }

        [JsonPropertyName("assumptions")]
        public List<string>? Assumptions
        {
            get; set;
        }

        [JsonPropertyName("priorManifestVersion")]
        public string? PriorManifestVersion
        {
            get; set;
        }
    }

    public sealed class InputsSection
    {
        [JsonPropertyName("brief")]
        public string Brief { get; set; } = "inputs/brief.md";
    }

    public sealed class OutputsSection
    {
        [JsonPropertyName("localCacheDir")]
        public string LocalCacheDir { get; set; } = "outputs";
    }

    public sealed class PluginsSection
    {
        [JsonPropertyName("lockFile")]
        public string LockFile { get; set; } = "plugins/plugin-lock.json";
    }

    public sealed class InfraSection
    {
        [JsonPropertyName("terraform")]
        public TerraformSection Terraform { get; set; } = new();
    }

    public sealed class TerraformSection
    {
        [JsonPropertyName("enabled")]
        public bool Enabled
        {
            get; set;
        }

        [JsonPropertyName("path")]
        public string Path { get; set; } = "infra/terraform";
    }

    private static string BuildArchiForgeJson(string projectName)
    {
        ArchiForgeConfig config = new()
        {
            ProjectName = projectName,
            ApiUrl = "http://localhost:5128",
            Infra = new InfraSection
            {
                Terraform = new TerraformSection
                {
                    Enabled = false,
                    Path = "infra/terraform"
                }
            },
            Architecture = new ArchitectureSection
            {
                Environment = "prod",
                CloudProvider = "Azure",
                Constraints = ["Private endpoints required", "Use managed identity"],
                RequiredCapabilities = ["Azure AI Search", "SQL", "Managed Identity"],
                Assumptions = ["Moderate query volume", "Internal enterprise usage only"]
            }
        };
        return JsonSerializer.Serialize(config, SJsonManifest) + Environment.NewLine;
    }

    public static ArchiForgeConfig LoadConfig(string? projectRoot)
    {
        string manifestPath = projectRoot != null ? Path.Combine(projectRoot, "archiforge.json") : "archiforge.json";
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("archiforge.json not found.", manifestPath);

        string json = File.ReadAllText(manifestPath, Encoding.UTF8);

        ArchiForgeConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<ArchiForgeConfig>(json, SJsonManifest);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid JSON in {manifestPath}: {ex.Message}", ex);
        }
        if (config is null)
            throw new InvalidDataException($"Unable to parse {manifestPath} into ArchiForgeConfig.");
        if (projectRoot != null)
            ValidateConfigOrThrow(config, projectRoot);
        return config;
    }

    private static void ValidateConfigOrThrow(ArchiForgeConfig config, string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(config.SchemaVersion))
            throw new InvalidDataException("archiforge.json: schemaVersion is required.");
        if (string.IsNullOrWhiteSpace(config.ProjectName))
            throw new InvalidDataException("archiforge.json: projectName is required.");
        if (config.Inputs is null || string.IsNullOrWhiteSpace(config.Inputs.Brief))
            throw new InvalidDataException("archiforge.json: inputs.brief is required.");
        if (config.Outputs is null || string.IsNullOrWhiteSpace(config.Outputs.LocalCacheDir))
            throw new InvalidDataException("archiforge.json: outputs.localCacheDir is required.");
        if (config.Plugins is null || string.IsNullOrWhiteSpace(config.Plugins.LockFile))
            throw new InvalidDataException("archiforge.json: plugins.lockFile is required.");
        if (config.Infra is null || config.Infra.Terraform is null)
            throw new InvalidDataException("archiforge.json: infra.terraform section is required.");

        EnsureRelativePathOrThrow(config.Inputs.Brief, "inputs.brief");
        EnsureRelativePathOrThrow(config.Outputs.LocalCacheDir, "outputs.localCacheDir");
        EnsureRelativePathOrThrow(config.Plugins.LockFile, "plugins.lockFile");
        EnsureRelativePathOrThrow(config.Infra.Terraform.Path, "infra.terraform.path");

        string briefPath = Path.Combine(projectRoot, config.Inputs.Brief);
        if (!File.Exists(briefPath))
            throw new FileNotFoundException($"Brief file not found at '{config.Inputs.Brief}'.", briefPath);
        string lockPath = Path.Combine(projectRoot, config.Plugins.LockFile);

        if (!File.Exists(lockPath))
            throw new FileNotFoundException($"Plugin lock file not found at '{config.Plugins.LockFile}'.", lockPath);

        if (!config.Infra.Terraform.Enabled)
            return;

        string tfDir = Path.Combine(projectRoot, config.Infra.Terraform.Path);

        if (!Directory.Exists(tfDir))
            throw new DirectoryNotFoundException($"Terraform directory not found at '{config.Infra.Terraform.Path}'.");
    }

    private static void EnsureRelativePathOrThrow(string path, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException($"archiforge.json: {fieldName} is empty.");
        if (Path.IsPathRooted(path))
            throw new InvalidDataException($"archiforge.json: {fieldName} must be a relative path, got rooted path '{path}'.");
        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("../", StringComparison.Ordinal) || normalized.Contains("/../"))
            throw new InvalidDataException($"archiforge.json: {fieldName} must not contain '..' segments ('{path}').");
    }

    private static string BuildBriefMd(string projectName)
    {
        return
            $@"# {projectName} — ArchiForge Brief

## Goal
Describe the outcome you want (business + technical). Keep it short and runnable.

## Constraints
- Security/compliance requirements:
- Time/budget:
- Data sensitivity:

## Inputs
- Source systems:
- Target environment:
- Key dependencies:

## Outputs
- What artifacts should ArchiForge generate?

## Acceptance Criteria
- What does ""done"" look like?
";
    }

    private static string BuildPluginLockJson()
    {
        const string lockDoc = """
                               {
                                               "schemaVersion": "1.0",
                                               "generatedUtc": "REPLACE_AT_RUNTIME",
                                               "plugins": [
                                                   {
                                                       "name": "example.generator.docs",
                                                       "image": "ghcr.io/your-org/archiforge-plugin-docs",
                                                       "version": "1.0.0",
                                                   "endpoint": "local"
                                                   }
                                               ]
                                           }
                               """;
        return lockDoc.Replace("REPLACE_AT_RUNTIME", DateTime.UtcNow.ToString("O")) + Environment.NewLine;
    }

    private static string BuildTerraformMainTf()
    {
        return
            """
            terraform {
              required_version = ">= 1.6.0"
            }

            # provider "azurerm" {
            #   features {}
            # }

            """;
    }

    private static string BuildTerraformVariablesTf()
    {
        return
            """
            # variable "location" {
            #   type        = string
            #   description = "Azure region"
            # }

            """;
    }

    private static string BuildDocsReadme(string projectName)
    {
        return
            $"""
             # {projectName}

             ## Folder layout

             - `archiforge.json` — The single source of truth for project configuration.
             - `inputs/brief.md` — The one thing you can always run (minimal project brief).
             - `outputs/` — Optional local cache of output artifacts (not authoritative). Includes `.gitkeep` to preserve the folder in Git.
             - `plugins/plugin-lock.json` — Pinned plugin images + versions + endpoints.
             - `infra/terraform/` — Optional; stubbed initially (`main.tf`, `variables.tf`).
             - `docs/` — Human documentation for the project.

             ## How to use

             1. Edit `inputs/brief.md`
             2. Update `archiforge.json` if needed
             3. Run ArchiForge against the brief (implementation-specific)

             """;
    }
}
