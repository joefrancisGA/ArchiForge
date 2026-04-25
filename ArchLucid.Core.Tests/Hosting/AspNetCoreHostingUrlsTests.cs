using ArchLucid.Host.Core.Hosting;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Core.Tests.Hosting;

public sealed class AspNetCoreHostingUrlsTests
{
    [Fact]
    public void ShouldUseHttpsRedirection_WhenUrlsMissing_ReturnsTrue()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        bool result = AspNetCoreHostingUrls.ShouldUseHttpsRedirection(configuration);

        Assert.True(result);
    }

    [Fact]
    public void ShouldUseHttpsRedirection_WhenHttpOnlyUrls_ReturnsFalse()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ASPNETCORE_URLS"] = "http://+:8080" })
            .Build();

        bool result = AspNetCoreHostingUrls.ShouldUseHttpsRedirection(configuration);

        Assert.False(result);
    }

    [Fact]
    public void ShouldUseHttpsRedirection_WhenMixedHttpAndHttps_ReturnsTrue()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_URLS"] = "http://localhost:5000;https://localhost:5001"
            })
            .Build();

        bool result = AspNetCoreHostingUrls.ShouldUseHttpsRedirection(configuration);

        Assert.True(result);
    }

    [Fact]
    public void ShouldUseHttpsRedirection_WhenNullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AspNetCoreHostingUrls.ShouldUseHttpsRedirection(null!));
    }
}
