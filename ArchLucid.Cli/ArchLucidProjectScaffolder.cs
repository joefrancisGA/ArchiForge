using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Data.SqlClient;


/*________________________________________
2) Folder layout created by archlucid new
    <projectName>/
archlucid.json
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
•	archlucid.json: the single source of truth for project configuration.
•	inputs/brief.md: the "one thing you can always run."
•	outputs/: optional local cache of output artifacts (not authoritative).
    •	plugins/plugin-lock.json: pinned plugin images + versions + endpoints.
•	infra/terraform/: optional; stubbed initially.
*/


namespace ArchLucid.Cli;

public static class ArchLucidProjectScaffolder
{
    /// <summary>Primary CLI manifest file name in each scaffolded project.</summary>
    public const string CliManifestFileName = "archlucid.json";

    /// <summary>Shared options for <see cref="CliManifestFileName" /> read/write (CA1869: single cached instance).</summary>
    private static readonly JsonSerializerOptions SJsonManifest = new()
    {
        WriteIndented = true, PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
    };

    public static string CreateProject(ScaffoldOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ProjectName))
            throw new ArgumentException("ProjectName is required. It cannot be null, empty, or whitespace.",
                nameof(options));

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
        WriteFile(Path.Combine(projectRoot, CliManifestFileName), BuildArchLucidJson(options.ProjectName),
            options.OverwriteExistingFiles);
        WriteFile(Path.Combine(projectRoot, "inputs", "brief.md"), BuildBriefMd(options.ProjectName),
            options.OverwriteExistingFiles);
        WriteFile(Path.Combine(projectRoot, "outputs", ".gitkeep"), "", options.OverwriteExistingFiles);
        WriteFile(Path.Combine(projectRoot, "plugins", "plugin-lock.json"), BuildPluginLockJson(),
            options.OverwriteExistingFiles);

        if (options.IncludeTerraformStubs)
        {
            WriteFile(Path.Combine(projectRoot, "infra", "terraform", "main.tf"), BuildTerraformMainTf(),
                options.OverwriteExistingFiles);
            WriteFile(Path.Combine(projectRoot, "infra", "terraform", "variables.tf"), BuildTerraformVariablesTf(),
                options.OverwriteExistingFiles);
        }

        WriteFile(Path.Combine(projectRoot, "docs", "README.md"),
            BuildDocsReadme(options.ProjectName, options.QuickStartEvaluation), options.OverwriteExistingFiles);

        if (options.QuickStartEvaluation)
            WriteQuickStartEvaluationArtifacts(projectRoot, options);

        if (options.RegisterProject)
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))

                throw new InvalidOperationException(
                    "ScaffoldOptions.ConnectionString must be set when RegisterProject is true. " +
                    "Set it explicitly; there is no hardcoded default connection string.");

            const string sqlQuery =
                "INSERT INTO PROJECTS (ProjectName, BaseDirectory, OverwriteExistingFiles, IncludeTerraformStubs) " +
                "VALUES (@ProjectName, @BaseDirectory, @OverwriteExistingFiles, @IncludeTerraformStubs)";

            try
            {
                using SqlConnection connection = new(options.ConnectionString);
                connection.Open();
                Console.WriteLine("Connection successful.");
                using SqlCommand command = new(sqlQuery, connection);
                command.Parameters.Add("@ProjectName", SqlDbType.NVarChar, 0).Value = options.ProjectName;
                command.Parameters.Add("@BaseDirectory", SqlDbType.NVarChar, 0).Value =
                    options.BaseDirectory ?? (object)DBNull.Value;
                command.Parameters.Add("@OverwriteExistingFiles", SqlDbType.Bit, 0).Value =
                    options.OverwriteExistingFiles;
                command.Parameters.Add("@IncludeTerraformStubs", SqlDbType.Bit, 0).Value =
                    options.IncludeTerraformStubs;
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

    private static void WriteQuickStartEvaluationArtifacts(string projectRoot, ScaffoldOptions options)
    {
        string localDir = Path.Combine(projectRoot, "local");
        CreateDirectory(localDir);
        string appsettingsPath = Path.Combine(localDir, "archlucid.quickstart.appsettings.json");
        WriteFile(appsettingsPath, BuildQuickstartAppsettingsJson(), options.OverwriteExistingFiles);
        string sqlitePath = Path.Combine(projectRoot,
            QuickStartSQLiteProjectRegistry.DefaultRelativeDbPath.Replace('/', Path.DirectorySeparatorChar));

        QuickStartSQLiteProjectRegistry.EnsureRegistered(Path.GetFullPath(sqlitePath), options.ProjectName,
            options.BaseDirectory, options.OverwriteExistingFiles, options.IncludeTerraformStubs);

        Console.WriteLine(
            "Quickstart: wrote " + appsettingsPath + " and " + sqlitePath
            + " (InMemory host storage; no SQL Server required for local evaluation — see docs/README.md).");
    }

    private static string BuildQuickstartAppsettingsJson()
    {
        return JsonSerializer.Serialize(new { ArchLucid = new { StorageProvider = "InMemory" } }, SJsonManifest)
               + Environment.NewLine;
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
        File.WriteAllText(path, contents, new UTF8Encoding(false));
    }

    private static string BuildArchLucidJson(string projectName)
    {
        ArchLucidCliConfig config = new()
        {
            ProjectName = projectName,
            ApiUrl = "http://localhost:5128",
            Infra =
                new InfraSection { Terraform = new TerraformSection { Enabled = false, Path = "infra/terraform" } },
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

    public static ArchLucidCliConfig LoadConfig(string? projectRoot)
    {
        string lucidPath = projectRoot is not null
            ? Path.Combine(projectRoot, CliManifestFileName)
            : CliManifestFileName;
        string legacyPath = projectRoot is not null
            ? Path.Combine(projectRoot, "archi" + "forge.json")
            : "archi" + "forge.json";

        string manifestPath;
        if (File.Exists(lucidPath))

            manifestPath = lucidPath;

        else if (File.Exists(legacyPath))
        {
            Console.Error.WriteLine(
                "[ArchLucid CLI] Using legacy manifest file name; rename '"
                + "archi"
                + "forge.json' to '"
                + CliManifestFileName
                + "'.");

            manifestPath = legacyPath;
        }
        else

            throw new FileNotFoundException(CliManifestFileName + " not found.", lucidPath);

        string json = File.ReadAllText(manifestPath, Encoding.UTF8);

        ArchLucidCliConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<ArchLucidCliConfig>(json, SJsonManifest);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid JSON in {manifestPath}: {ex.Message}", ex);
        }

        if (config is null)
            throw new InvalidDataException($"Unable to parse {manifestPath} into ArchLucidCliConfig.");
        if (projectRoot is not null)
            ValidateConfigOrThrow(config, projectRoot);
        return config;
    }

    private static void ValidateConfigOrThrow(ArchLucidCliConfig config, string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(config.SchemaVersion))
            throw new InvalidDataException(CliManifestFileName + ": schemaVersion is required.");
        if (string.IsNullOrWhiteSpace(config.ProjectName))
            throw new InvalidDataException(CliManifestFileName + ": projectName is required.");
        if (config.Inputs is null || string.IsNullOrWhiteSpace(config.Inputs.Brief))
            throw new InvalidDataException(CliManifestFileName + ": inputs.brief is required.");
        if (config.Outputs is null || string.IsNullOrWhiteSpace(config.Outputs.LocalCacheDir))
            throw new InvalidDataException(CliManifestFileName + ": outputs.localCacheDir is required.");

        EnsureRelativePathOrThrow(config.Inputs.Brief, "inputs.brief");
        EnsureRelativePathOrThrow(config.Outputs.LocalCacheDir, "outputs.localCacheDir");

        string briefPath = Path.Combine(projectRoot, config.Inputs.Brief);
        if (!File.Exists(briefPath))
            throw new FileNotFoundException($"Brief file not found at '{config.Inputs.Brief}'.", briefPath);

        if (config.Plugins is not null && !string.IsNullOrWhiteSpace(config.Plugins.LockFile))
        {
            EnsureRelativePathOrThrow(config.Plugins.LockFile, "plugins.lockFile");
            string lockPath = Path.Combine(projectRoot, config.Plugins.LockFile);

            if (!File.Exists(lockPath))
                throw new FileNotFoundException($"Plugin lock file not found at '{config.Plugins.LockFile}'.",
                    lockPath);
        }

        InfraSection infra = config.Infra ?? new InfraSection();
        TerraformSection tf = infra.Terraform;

        if (!tf.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(tf.Path))
            throw new InvalidDataException(CliManifestFileName +
                                           ": infra.terraform.path is required when infra.terraform.enabled is true.");

        EnsureRelativePathOrThrow(tf.Path, "infra.terraform.path");
        string tfDir = Path.Combine(projectRoot, tf.Path);

        if (!Directory.Exists(tfDir))
            throw new DirectoryNotFoundException($"Terraform directory not found at '{tf.Path}'.");
    }

    private static void EnsureRelativePathOrThrow(string path, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException($"{CliManifestFileName}: {fieldName} is empty.");
        if (Path.IsPathRooted(path))
            throw new InvalidDataException(
                $"{CliManifestFileName}: {fieldName} must be a relative path, got rooted path '{path}'.");
        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("../", StringComparison.Ordinal) || normalized.Contains("/../"))
            throw new InvalidDataException(
                $"{CliManifestFileName}: {fieldName} must not contain '..' segments ('{path}').");
    }

    private static string BuildBriefMd(string projectName)
    {
        return
            $@"# {projectName} — ArchLucid Brief

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
- What artifacts should ArchLucid generate?

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
                                                       "image": "ghcr.io/your-org/archlucid-plugin-docs",
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

    private static string BuildDocsReadme(string projectName, bool quickStartEvaluation)
    {
        string body =
            $"""
             # {projectName}

             ## Folder layout

             - `archlucid.json` — The single source of truth for project configuration.
             - `inputs/brief.md` — The one thing you can always run (minimal project brief).
             - `outputs/` — Optional local cache of output artifacts (not authoritative). Includes `.gitkeep` to preserve the folder in Git.
             - `plugins/plugin-lock.json` — Pinned plugin images + versions + endpoints.
             - `infra/terraform/` — Optional; stubbed initially (`main.tf`, `variables.tf`).
             - `docs/` — Human documentation for the project.

             ## How to use

             1. Edit `inputs/brief.md`
             2. Update `archlucid.json` if needed
             3. Run `archlucid run` (or your host workflow) against the brief

             """;

        if (!quickStartEvaluation)
            return body;

        return body
               + $"""

             ## Quickstart evaluation (no SQL Server)

             This project was scaffolded with `archlucid new {projectName} --quickstart`.

             - `local/archlucid.quickstart.appsettings.json` — sets `ArchLucid:StorageProvider` to `InMemory` so ArchLucid hosts can run **without** SQL Server. Merge into your configuration (for example `appsettings.Development.json`), or run `dotnet user-secrets set "ArchLucid:StorageProvider" "InMemory"` for the API project.
             - `local/archlucid-evaluation.sqlite` — file-backed SQLite used only as a **CLI-side** project registry for this scaffold. The product database is still controlled by `ArchLucid:StorageProvider` (`InMemory` or `Sql` with SQL Server).

             """;
    }

    public sealed class ScaffoldOptions
    {
        public string ProjectName
        {
            get;
            set;
        } = "";

        public string? BaseDirectory
        {
            get;
            set;
        } = null;

        public bool OverwriteExistingFiles
        {
            get;
            set;
        } = false;

        public bool IncludeTerraformStubs
        {
            get;
            set;
        } = true;

        /// <summary>
        ///     When true, attempt to register the project in SQL Server (PROJECTS table).
        ///     Default false so scaffolding works without a database connection.
        /// </summary>
        public bool RegisterProject
        {
            get;
            set;
        } = false;

        /// <summary>
        ///     SQL Server connection string used when <see cref="RegisterProject" /> is true.
        ///     Must be set explicitly; there is no hardcoded default to avoid accidental production writes.
        ///     Example: "Server=localhost;Database=ArchLucid;Trusted_Connection=True;"
        /// </summary>
        public string? ConnectionString
        {
            get;
            set;
        } = null;

        /// <summary>
        ///     When true, write quickstart evaluation artifacts under <c>local/</c>: a SQLite file (<c>
        ///     local/archlucid-evaluation.sqlite</c>) for CLI-side project registry, and an appsettings JSON fragment
        ///     setting <c>ArchLucid:StorageProvider</c> to <c>InMemory</c> so hosts do not require SQL Server for
        ///     initial evaluation.
        /// </summary>
        public bool QuickStartEvaluation
        {
            get;
            set;
        } = false;
    }

    public sealed class ArchLucidCliConfig
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion
        {
            get;
            set;
        } = "1.0";

        [JsonPropertyName("projectName")]
        public string ProjectName
        {
            get;
            set;
        } = "";

        [JsonPropertyName("apiUrl")]
        public string? ApiUrl
        {
            get;
            set;
        }

        [JsonPropertyName("inputs")]
        public InputsSection Inputs
        {
            get;
            set;
        } = new();

        [JsonPropertyName("outputs")]
        public OutputsSection Outputs
        {
            get;
            set;
        } = new();

        /// <summary>Optional — when omitted, CLI skips plugin lock validation (no plugin directory required).</summary>
        [JsonPropertyName("plugins")]
        public PluginsSection? Plugins
        {
            get;
            set;
        }

        /// <summary>Optional — when omitted, Terraform path checks are skipped (treated as disabled).</summary>
        [JsonPropertyName("infra")]
        public InfraSection? Infra
        {
            get;
            set;
        }

        [JsonPropertyName("architecture")]
        public ArchitectureSection? Architecture
        {
            get;
            set;
        }

        [JsonPropertyName("httpResilience")]
        public CliHttpResilienceConfig? HttpResilience
        {
            get;
            set;
        }
    }

    /// <summary>Optional HTTP retry tuning for the CLI API client (<c>archlucid.json</c>).</summary>
    public sealed class CliHttpResilienceConfig
    {
        [JsonPropertyName("maxRetryAttempts")]
        public int? MaxRetryAttempts
        {
            get;
            set;
        }

        [JsonPropertyName("initialDelaySeconds")]
        public int? InitialDelaySeconds
        {
            get;
            set;
        }
    }

    public sealed class ArchitectureSection
    {
        [JsonPropertyName("environment")]
        public string? Environment
        {
            get;
            set;
        }

        [JsonPropertyName("cloudProvider")]
        public string? CloudProvider
        {
            get;
            set;
        }

        [JsonPropertyName("constraints")]
        public List<string>? Constraints
        {
            get;
            set;
        }

        [JsonPropertyName("requiredCapabilities")]
        public List<string>? RequiredCapabilities
        {
            get;
            set;
        }

        [JsonPropertyName("assumptions")]
        public List<string>? Assumptions
        {
            get;
            set;
        }

        [JsonPropertyName("priorManifestVersion")]
        public string? PriorManifestVersion
        {
            get;
            set;
        }
    }

    public sealed class InputsSection
    {
        [JsonPropertyName("brief")]
        public string Brief
        {
            get;
            set;
        } = "inputs/brief.md";
    }

    public sealed class OutputsSection
    {
        [JsonPropertyName("localCacheDir")]
        public string LocalCacheDir
        {
            get;
            set;
        } = "outputs";
    }

    public sealed class PluginsSection
    {
        [JsonPropertyName("lockFile")]
        public string LockFile
        {
            get;
            set;
        } = "plugins/plugin-lock.json";
    }

    public sealed class InfraSection
    {
        [JsonPropertyName("terraform")]
        public TerraformSection Terraform
        {
            get;
            set;
        } = new();
    }

    public sealed class TerraformSection
    {
        [JsonPropertyName("enabled")]
        public bool Enabled
        {
            get;
            set;
        }

        [JsonPropertyName("path")]
        public string Path
        {
            get;
            set;
        } = "infra/terraform";
    }
}
