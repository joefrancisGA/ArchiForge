using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ArchLucid.Cli.Diagnostics;

/// <summary>
///     Builds <see cref="IConfiguration" /> for operator CLI checks: optional <c>appsettings*.json</c>
///     in the current directory, then environment variables (later sources win).
/// </summary>
internal static class DoctorLocalConfiguration
{
    internal static IConfiguration CreateForDoctor()
    {
        ConfigurationBuilder builder = new();
        string cwd = Directory.GetCurrentDirectory();

        builder.AddJsonFile(Path.Combine(cwd, "appsettings.json"), optional: true, reloadOnChange: false);

        string envName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environments.Production;

        builder.AddJsonFile(Path.Combine(cwd, $"appsettings.{envName}.json"), optional: true, reloadOnChange: false);
        builder.AddEnvironmentVariables();

        return builder.Build();
    }
}
