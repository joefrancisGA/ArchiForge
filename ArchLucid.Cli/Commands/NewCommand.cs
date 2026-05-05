namespace ArchLucid.Cli.Commands;

internal static class NewCommand
{
    /// <summary>
    ///     Scaffolds a new ArchLucid project directory. Optional <paramref name="quickStartEvaluation" /> writes
    ///     <c>local/archlucid.quickstart.appsettings.json</c> (<c>InMemory</c> storage) and
    ///     <c>local/archlucid-evaluation.sqlite</c> so evaluators can start without SQL Server; see CLI help text.
    /// </summary>
    public static Task<int> RunAsync(string projectName, bool quickStartEvaluation)
    {
        Console.WriteLine("Creating ArchLucid project " + projectName);

        ArchLucidProjectScaffolder.ScaffoldOptions scaffoldOptions = new()
        {
            ProjectName = projectName,
            BaseDirectory = null,
            OverwriteExistingFiles = true,
            IncludeTerraformStubs = true,
            QuickStartEvaluation = quickStartEvaluation
        };

        ArchLucidProjectScaffolder.CreateProject(scaffoldOptions);

        return Task.FromResult(CliExitCode.Success);
    }
}
