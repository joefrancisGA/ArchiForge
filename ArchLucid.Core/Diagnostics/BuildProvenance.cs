using System.Reflection;
using System.Runtime.InteropServices;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Immutable build and runtime facts for a host assembly (API, CLI, workers).
///     Used for startup logs, OpenTelemetry <c>service.version</c>, <c>GET /version</c>, CLI <c>doctor</c>,
///     and support handoffs.
/// </summary>
public sealed record BuildProvenance(
    string InformationalVersion,
    string AssemblyVersion,
    string? FileVersion,
    string RuntimeFrameworkDescription,
    string? CommitSha)
{
    /// <summary>
    ///     Resolves provenance from <paramref name="assembly" /> (typically the entry or host assembly).
    ///     When the .NET SDK property <c>SourceRevisionId</c> is set at build time, the informational
    ///     version contains a <c>+{sha}</c> suffix that this method extracts into <see cref="CommitSha" />.
    /// </summary>
    public static BuildProvenance FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        AssemblyName name = assembly.GetName();
        string assemblyVersion = name.Version?.ToString() ?? "unknown";

        string informational =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assemblyVersion;

        string? fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        string? commitSha = ParseCommitSha(informational);

        return new BuildProvenance(
            informational,
            assemblyVersion,
            fileVersion,
            RuntimeInformation.FrameworkDescription,
            commitSha);
    }

    /// <summary>
    ///     Extracts the commit SHA from the <c>+{sha}</c> suffix of an informational version string.
    ///     Returns <c>null</c> when no suffix is present.
    /// </summary>
    internal static string? ParseCommitSha(string informationalVersion)
    {
        int plusIndex = informationalVersion.LastIndexOf('+');

        if (plusIndex < 0 || plusIndex == informationalVersion.Length - 1)
            return null;

        string candidate = informationalVersion[(plusIndex + 1)..];

        return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
    }
}
