using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests.Demo;

/// <summary>
///     Greenfield SQL API host with <c>Demo:AnonymousViewer:Enabled=true</c> for anonymous viewer integration tests.
/// </summary>
public sealed class DemoViewerEnabledSqlApiFactory : GreenfieldSqlApiFactory
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("Demo:AnonymousViewer:Enabled", "true");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Demo:AnonymousViewer:Enabled"] = "true" });
        });
    }
}
