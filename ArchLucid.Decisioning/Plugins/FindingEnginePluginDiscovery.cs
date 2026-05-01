using System.Reflection;

using ArchLucid.Decisioning.Interfaces;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Decisioning.Plugins;

/// <summary>
///     Discovers <see cref="IFindingEngine" /> implementations in a plugin directory (parameterless constructors only).
/// </summary>
public static class FindingEnginePluginDiscovery
{
    /// <summary>Built-in engine <see cref="IFindingEngine.EngineType" /> values — plugins with the same id are skipped.</summary>
    public static HashSet<string> BuiltInEngineTypeIds
    {
        get;
    } =
    [
        "requirement",
        "topology-coverage",
        "security-baseline",
        "security-coverage",
        "policy-applicability",
        "policy-coverage",
        "requirement-coverage",
        "compliance",
        "cost-constraint"
    ];

    /// <summary>
    ///     Returns concrete <see cref="IFindingEngine" /> types that can be registered as scoped services.
    /// </summary>
    /// <param name="pluginDirectory">Absolute or relative path; empty or missing directory yields no results.</param>
    public static IReadOnlyList<Type> Discover(string? pluginDirectory, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(pluginDirectory))
            return [];

        string fullPath = Path.GetFullPath(pluginDirectory);

        if (!Directory.Exists(fullPath))
        {
            if (logger.IsEnabled(LogLevel.Debug))

                logger.LogDebug("Finding engine plugin directory does not exist: {Path}", fullPath);

            return [];
        }

        List<Type> result = [];
        HashSet<string> seenEngineTypes = new(StringComparer.OrdinalIgnoreCase);

        foreach (string dllPath in Directory.EnumerateFiles(fullPath, "*.dll", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(dllPath);

            if (fileName.StartsWith("ArchLucid.", StringComparison.OrdinalIgnoreCase))
            {
                if (logger.IsEnabled(LogLevel.Debug))

                    logger.LogDebug("Skipping core assembly in plugin scan: {File}", fileName);

                continue;
            }

            Assembly assembly;

            try
            {
                assembly = Assembly.LoadFrom(dllPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load potential finding-engine plugin assembly: {Path}", dllPath);
                continue;
            }

            foreach (Type candidate in SafeGetExportedTypes(assembly))
            {
                if (!candidate.IsClass || candidate.IsAbstract)
                    continue;

                if (!typeof(IFindingEngine).IsAssignableFrom(candidate))
                    continue;

                if (candidate.GetConstructor(Type.EmptyTypes) is null)
                {
                    if (logger.IsEnabled(LogLevel.Debug))

                        logger.LogDebug(
                            "Skipping {TypeName}: IFindingEngine plugin must expose a parameterless constructor.",
                            candidate.FullName);

                    continue;
                }

                IFindingEngine? probe;

                try
                {
                    probe = Activator.CreateInstance(candidate) as IFindingEngine;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to instantiate finding-engine plugin type {TypeName} from {Assembly}.",
                        candidate.FullName,
                        fileName);
                    continue;
                }

                if (probe is null)
                    continue;

                string engineTypeId = probe.EngineType;

                if (string.IsNullOrWhiteSpace(engineTypeId))
                {
                    logger.LogWarning(
                        "Skipping {TypeName}: EngineType is empty.",
                        candidate.FullName);
                    continue;
                }

                if (BuiltInEngineTypeIds.Contains(engineTypeId))
                {
                    logger.LogWarning(
                        "Skipping plugin {TypeName}: EngineType '{EngineType}' collides with a built-in engine.",
                        candidate.FullName,
                        engineTypeId);
                    continue;
                }

                if (!seenEngineTypes.Add(engineTypeId))
                {
                    logger.LogWarning(
                        "Skipping duplicate plugin EngineType '{EngineType}' ({TypeName}).",
                        engineTypeId,
                        candidate.FullName);
                    continue;
                }

                result.Add(candidate);

                if (logger.IsEnabled(LogLevel.Information))

                    logger.LogInformation(
                        "Registered finding-engine plugin: EngineType={EngineType}, Category={Category}, Type={TypeName}, Assembly={Assembly}",
                        probe.EngineType,
                        probe.Category,
                        candidate.FullName,
                        fileName);
            }
        }

        return result;
    }

    private static IEnumerable<Type> SafeGetExportedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(static t => t is not null).Cast<Type>();
        }
    }
}
