using ArchLucid.Host.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ArchLucid.Core.Tests.Configuration;

[Trait("Category", "Unit")]
public sealed class HostEnvironmentClassificationTests
{
    [Fact]
    public void IsProductionOrStagingLike_IsTrue_for_IsProduction()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        IHostEnvironment env = new TestHostEnvironment(Environments.Production);

        bool result = HostEnvironmentClassification.IsProductionOrStagingLike(env, configuration);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsProductionOrStagingLike_IsTrue_for_IsStaging()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        IHostEnvironment env = new TestHostEnvironment(Environments.Staging);

        bool result = HostEnvironmentClassification.IsProductionOrStagingLike(env, configuration);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsProductionOrStagingLike_IsTrue_when_configuration_ARCHLUCID_ENVIRONMENT_is_Production()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ARCHLUCID_ENVIRONMENT"] = "Production" })
            .Build();
        IHostEnvironment env = new TestHostEnvironment(Environments.Development);

        bool result = HostEnvironmentClassification.IsProductionOrStagingLike(env, configuration);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsProductionOrStagingLike_IsTrue_when_configuration_ARCHLUCID_ENVIRONMENT_is_Staging_case_insensitive()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ARCHLUCID_ENVIRONMENT"] = "staging" })
            .Build();
        IHostEnvironment env = new TestHostEnvironment(Environments.Development);

        bool result = HostEnvironmentClassification.IsProductionOrStagingLike(env, configuration);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsProductionOrStagingLike_IsFalse_for_development_without_production_like_override()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        IHostEnvironment env = new TestHostEnvironment(Environments.Development);

        bool result = HostEnvironmentClassification.IsProductionOrStagingLike(env, configuration);

        result.Should().BeFalse();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName
        {
            get;
            set;
        }

        public string ApplicationName
        {
            get;
            set;
        } = "ArchLucid.Core.Tests";

        public string ContentRootPath
        {
            get;
            set;
        } = "/";

        public IFileProvider ContentRootFileProvider
        {
            get;
            set;
        } = new NullFileProvider();
    }
}
