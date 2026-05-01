using ArchLucid.Host.Core.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Startup;

/// <summary>Warns when graded orphan enforcement stays in <see cref="DataConsistencyEnforcementMode.Warn"/> on non-dev hosts (no paging counter).</summary>
public sealed class DataConsistencyEnforcementWarnModeProductionPostConfigure(
    IHostEnvironment hostEnvironment,
    ILogger<DataConsistencyEnforcementWarnModeProductionPostConfigure> logger)
    : IPostConfigureOptions<DataConsistencyEnforcementOptions>
{
    private readonly IHostEnvironment _hostEnvironment =
        hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));

    private readonly ILogger<DataConsistencyEnforcementWarnModeProductionPostConfigure> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public void PostConfigure(string? name, DataConsistencyEnforcementOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Mode != DataConsistencyEnforcementMode.Warn)
            return;

        if (_hostEnvironment.IsDevelopment())
            return;

        if (string.Equals(_hostEnvironment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
            return;


        if (_logger.IsEnabled(LogLevel.Warning))

            _logger.LogWarning(
                "DataConsistency:Enforcement:Mode is Warn on host environment {Environment}. Orphan detection will not increment archlucid_data_consistency_alerts_total; consider Alert or Quarantine after runbook review.",
                _hostEnvironment.EnvironmentName);
    }
}
