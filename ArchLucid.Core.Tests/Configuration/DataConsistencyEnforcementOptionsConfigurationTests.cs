using ArchLucid.Host.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Core.Tests.Configuration;

public sealed class DataConsistencyEnforcementOptionsConfigurationTests
{
    [Fact]
    public void DataConsistency_Enforcement_section_binds_AutoQuarantine()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["DataConsistency:Enforcement:Mode"] = "Alert",
                    ["DataConsistency:Enforcement:AutoQuarantine"] = "true",
                })
            .Build();

        DataConsistencyEnforcementOptions options = new()
        {
            Mode = Enum.Parse<DataConsistencyEnforcementMode>(
                configuration["DataConsistency:Enforcement:Mode"] ?? nameof(DataConsistencyEnforcementMode.Warn),
                ignoreCase: true),
            AutoQuarantine = bool.Parse(configuration["DataConsistency:Enforcement:AutoQuarantine"] ?? "false"),
        };

        options.Mode.Should().Be(DataConsistencyEnforcementMode.Alert);
        options.AutoQuarantine.Should().BeTrue();
    }

    [Fact]
    public void DataConsistency_Enforcement_defaults_AutoQuarantine_false()
    {
        DataConsistencyEnforcementOptions options = new();

        options.AutoQuarantine.Should().BeFalse();
    }
}
