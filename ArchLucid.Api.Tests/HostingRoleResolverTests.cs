using ArchLucid.Host.Core.Hosting;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

public sealed class HostingRoleResolverTests
{
    [Fact]
    public void Resolve_WhenMissing_returns_Combined()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        HostingRoleResolver.Resolve(configuration).Should().Be(ArchLucidHostingRole.Combined);
    }

    [Theory]
    [InlineData("Api", ArchLucidHostingRole.Api)]
    [InlineData("api", ArchLucidHostingRole.Api)]
    [InlineData("Worker", ArchLucidHostingRole.Worker)]
    [InlineData("WORKER", ArchLucidHostingRole.Worker)]
    [InlineData("Combined", ArchLucidHostingRole.Combined)]
    [InlineData("bogus", ArchLucidHostingRole.Combined)]
    public void Resolve_WhenSet_returns_expected_role(string raw, ArchLucidHostingRole expected)
    {
        Dictionary<string, string?> data = new() { ["Hosting:Role"] = raw };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        HostingRoleResolver.Resolve(configuration).Should().Be(expected);
    }
}
