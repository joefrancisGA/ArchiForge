using ArchiForge.Api.Hosting;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchiForge.Api.Tests;

public sealed class HostingRoleResolverTests
{
    [Fact]
    public void Resolve_WhenMissing_returns_Combined()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        HostingRoleResolver.Resolve(configuration).Should().Be(ArchiForgeHostingRole.Combined);
    }

    [Theory]
    [InlineData("Api", ArchiForgeHostingRole.Api)]
    [InlineData("api", ArchiForgeHostingRole.Api)]
    [InlineData("Worker", ArchiForgeHostingRole.Worker)]
    [InlineData("WORKER", ArchiForgeHostingRole.Worker)]
    [InlineData("Combined", ArchiForgeHostingRole.Combined)]
    [InlineData("bogus", ArchiForgeHostingRole.Combined)]
    public void Resolve_WhenSet_returns_expected_role(string raw, ArchiForgeHostingRole expected)
    {
        Dictionary<string, string?> data = new() { ["Hosting:Role"] = raw };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data!).Build();

        HostingRoleResolver.Resolve(configuration).Should().Be(expected);
    }
}
