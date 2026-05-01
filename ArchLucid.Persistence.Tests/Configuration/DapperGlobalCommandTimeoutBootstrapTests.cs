using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Configuration;

using Dapper;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Persistence.Tests.Configuration;

[Trait("Category", "Unit")]
public sealed class DapperGlobalCommandTimeoutBootstrapTests
{
    private static readonly Lock SCommandTimeoutGate = new();

    [SkippableFact]
    public void ApplyIfConfigured_Throws_WhenConfigurationNull()
    {
        Action act = () => DapperGlobalCommandTimeoutBootstrap.ApplyIfConfigured(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [SkippableFact]
    public void ApplyIfConfigured_NoOp_WhenSecondsMissingOrNonPositive()
    {
        lock (SCommandTimeoutGate)
        {
            int? before = SqlMapper.Settings.CommandTimeout;

            try
            {
                IConfiguration empty = new ConfigurationBuilder().AddInMemoryCollection().Build();
                DapperGlobalCommandTimeoutBootstrap.ApplyIfConfigured(empty);

                IConfiguration zero = new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        [new($"{ArchLucidPersistenceOptions.SectionPath}:DefaultSqlCommandTimeoutSeconds", "0")])
                    .Build();
                DapperGlobalCommandTimeoutBootstrap.ApplyIfConfigured(zero);
            }
            finally
            {
                SqlMapper.Settings.CommandTimeout = before;
            }
        }
    }

    [SkippableFact]
    public void ApplyIfConfigured_SetsSqlMapper_WhenPositive()
    {
        lock (SCommandTimeoutGate)
        {
            int? before = SqlMapper.Settings.CommandTimeout;

            try
            {
                IConfiguration config = new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        [new($"{ArchLucidPersistenceOptions.SectionPath}:DefaultSqlCommandTimeoutSeconds", "44")])
                    .Build();

                DapperGlobalCommandTimeoutBootstrap.ApplyIfConfigured(config);

                SqlMapper.Settings.CommandTimeout.Should().Be(44);
            }
            finally
            {
                SqlMapper.Settings.CommandTimeout = before;
            }
        }
    }
}
