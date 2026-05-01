using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests.Security;

/// <summary>
///     Greenfield SQL host with <c>SqlServer:RowLevelSecurity:ApplySessionContext</c> enabled so per-request
///     <c>x-tenant-id</c> / workspace / project headers map to <c>SESSION_CONTEXT</c> and authority runs are RLS-scoped.
/// </summary>
public sealed class SqlRlsTenantIsolationApiFactory : GreenfieldSqlApiFactory
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("SqlServer:RowLevelSecurity:ApplySessionContext", "true");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "true" });
        });
    }
}
