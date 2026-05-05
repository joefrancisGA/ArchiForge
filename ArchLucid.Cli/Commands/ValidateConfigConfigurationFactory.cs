using Microsoft.Extensions.Configuration;

namespace ArchLucid.Cli.Commands;

internal static class ValidateConfigConfigurationFactory
{
    internal static bool AppsettingsFileExists(string contentRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentRoot);

        return File.Exists(Path.Combine(contentRoot, "appsettings.json"));
    }

    /// <summary>
    ///     Merges JSON files (including environment-specific overlay), CLI overlays, then environment variables — last wins.
    /// </summary>
    internal static IConfiguration BuildMerged(ArchLucidProjectScaffolder.ArchLucidCliConfig? cli)
    {
        List<KeyValuePair<string, string?>> overlays = new(2);

        if (cli is not null && !string.IsNullOrWhiteSpace(cli.ApiUrl))

            overlays.Add(new KeyValuePair<string, string?>(
                "ARCHLUCID_API_URL",
                cli.ApiUrl.Trim().TrimEnd('/')));

        string contentRoot = Directory.GetCurrentDirectory();

        // Bootstrap ASP.NET environment name so appsettings.{env}.json participates like the API host.
        IConfiguration bootstrap = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        string hostingEnvironment =
            bootstrap["ASPNETCORE_ENVIRONMENT"]?.Trim()
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        string envJsonPath = $"appsettings.{hostingEnvironment}.json";

        return new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("archlucid.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile(envJsonPath, optional: true, reloadOnChange: false)
            .AddInMemoryCollection(overlays)
            .AddEnvironmentVariables()
            .Build();
    }
}
