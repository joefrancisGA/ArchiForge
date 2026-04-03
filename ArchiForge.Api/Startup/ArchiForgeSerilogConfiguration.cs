using System.Reflection;

using ArchiForge.Core.Diagnostics;

using Serilog;

namespace ArchiForge.Api.Startup;

/// <summary>Shared Serilog bootstrap for API and Worker web hosts.</summary>
internal static class ArchiForgeSerilogConfiguration
{
    internal static void Configure(WebApplicationBuilder builder, string applicationDisplayName)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            Assembly entryAssembly = typeof(ArchiForgeSerilogConfiguration).Assembly;
            BuildProvenance build = BuildProvenance.FromAssembly(entryAssembly);

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationDisplayName)
                .Enrich.WithProperty("AssemblyInformationalVersion", build.InformationalVersion)
                .Enrich.WithProperty("AssemblyFileVersion", build.FileVersion ?? string.Empty);
        });
    }
}
