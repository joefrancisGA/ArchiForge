namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Lightweight, non-secret build identity payload returned by <c>GET /version</c>
///     and included in CLI <c>doctor</c> output for support handoff.
/// </summary>
public sealed class BuildInfoResponse
{
    public string Application
    {
        get;
        init;
    } = string.Empty;

    public string InformationalVersion
    {
        get;
        init;
    } = string.Empty;

    public string AssemblyVersion
    {
        get;
        init;
    } = string.Empty;

    public string? FileVersion
    {
        get;
        init;
    }

    public string? CommitSha
    {
        get;
        init;
    }

    public string RuntimeFramework
    {
        get;
        init;
    } = string.Empty;

    public string Environment
    {
        get;
        init;
    } = string.Empty;

    /// <summary>
    ///     Creates a <see cref="BuildInfoResponse" /> from <paramref name="provenance" />
    ///     and optional environment metadata.
    /// </summary>
    public static BuildInfoResponse FromProvenance(
        BuildProvenance provenance,
        string applicationName,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(provenance);

        return new BuildInfoResponse
        {
            Application = applicationName,
            InformationalVersion = provenance.InformationalVersion,
            AssemblyVersion = provenance.AssemblyVersion,
            FileVersion = provenance.FileVersion,
            CommitSha = provenance.CommitSha,
            RuntimeFramework = provenance.RuntimeFrameworkDescription,
            Environment = environmentName
        };
    }
}
