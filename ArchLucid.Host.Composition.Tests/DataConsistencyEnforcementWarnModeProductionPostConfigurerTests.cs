using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Startup;

using FluentAssertions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Composition.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DataConsistencyEnforcementWarnModeProductionPostConfigurerTests
{
    [Fact]
    public void PostConfigure_Warn_on_staging_appends_startup_warning_when_enabled_at_warning_level()
    {
        List<string> warnings = [];
        CaptureWarnLogger<DataConsistencyEnforcementWarnModeProductionPostConfigure> logger =
            new(warnings, LogLevel.Warning);

        DataConsistencyEnforcementWarnModeProductionPostConfigure sut = new(
            new CompositionTestHostEnvironment(Environments.Staging),
            logger);

        sut.PostConfigure(null, new DataConsistencyEnforcementOptions { Mode = DataConsistencyEnforcementMode.Warn });

        warnings.Should()
            .ContainSingle(s => s.Contains("DataConsistency:Enforcement", StringComparison.Ordinal)
                && s.Contains("archlucid_data_consistency_alerts_total", StringComparison.Ordinal));
    }

    [Fact]
    public void PostConfigure_Warn_on_development_does_not_log()
    {
        List<string> warnings = [];
        CaptureWarnLogger<DataConsistencyEnforcementWarnModeProductionPostConfigure> logger =
            new(warnings, LogLevel.Warning);

        DataConsistencyEnforcementWarnModeProductionPostConfigure sut = new(
            new CompositionTestHostEnvironment(Environments.Development),
            logger);

        sut.PostConfigure(
            null,
            new DataConsistencyEnforcementOptions { Mode = DataConsistencyEnforcementMode.Warn });

        warnings.Should().BeEmpty();
    }

    [Fact]
    public void PostConfigure_Warn_when_environment_name_is_Testing_does_not_log()
    {
        List<string> warnings = [];
        CaptureWarnLogger<DataConsistencyEnforcementWarnModeProductionPostConfigure> logger =
            new(warnings, LogLevel.Warning);

        DataConsistencyEnforcementWarnModeProductionPostConfigure sut = new(
            new CompositionTestHostEnvironment("Testing"),
            logger);

        sut.PostConfigure(
            null,
            new DataConsistencyEnforcementOptions { Mode = DataConsistencyEnforcementMode.Warn });

        warnings.Should().BeEmpty();
    }

    [Fact]
    public void PostConfigure_Alert_does_not_log()
    {
        List<string> warnings = [];
        CaptureWarnLogger<DataConsistencyEnforcementWarnModeProductionPostConfigure> logger =
            new(warnings, LogLevel.Warning);

        DataConsistencyEnforcementWarnModeProductionPostConfigure sut = new(
            new CompositionTestHostEnvironment(Environments.Staging),
            logger);

        sut.PostConfigure(
            null,
            new DataConsistencyEnforcementOptions { Mode = DataConsistencyEnforcementMode.Alert });

        warnings.Should().BeEmpty();
    }

    private sealed class CaptureWarnLogger<T> : ILogger<T>
    {
        private readonly List<string> _warnings;

        private readonly LogLevel _minLevel;

        public CaptureWarnLogger(List<string> warnings, LogLevel minLevel)
        {
            _warnings = warnings;
            _minLevel = minLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => DisposableScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;


            string message = formatter(state, exception);

            _warnings.Add(message);
        }
    }

    private sealed class DisposableScope : IDisposable
    {
        public static readonly DisposableScope Instance = new();

        public void Dispose()
        {
        }
    }
}
