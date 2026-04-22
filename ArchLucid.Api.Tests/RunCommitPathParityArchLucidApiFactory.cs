using Microsoft.AspNetCore.Hosting;

namespace ArchLucid.Api.Tests;

/// <summary>
/// API host for ADR 0030 PR A2 cohort parity — toggles <see cref="ArchLucid.Core.Configuration.LegacyRunCommitPathOptions.LegacyRunCommitPath"/>.
/// </summary>
public sealed class RunCommitPathParityArchLucidApiFactory(bool legacyRunCommitPath) : ArchLucidApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Coordinator:LegacyRunCommitPath", legacyRunCommitPath ? "true" : "false");
    }
}
