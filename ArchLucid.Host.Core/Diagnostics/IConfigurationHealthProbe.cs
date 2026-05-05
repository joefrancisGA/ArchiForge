namespace ArchLucid.Host.Core.Diagnostics;

public interface IConfigurationHealthProbe
{
    Task<ConfigurationHealthReport> ProbeAsync(CancellationToken cancellationToken = default);
}
