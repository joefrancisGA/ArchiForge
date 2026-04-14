using ArchLucid.Core.Authorization;
using ArchLucid.Host.Core.Startup;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Composition.Tests;

/// <summary>Ensures <see cref="ArchLucidAuthorizationPoliciesExtensions.AddArchLucidAuthorizationPolicies"/> registers expected policies.</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArchLucidAuthorizationPoliciesRegistrationTests
{
    [Fact]
    public void AddArchLucidAuthorizationPolicies_registers_core_role_and_auditor_policies()
    {
        ServiceCollection services = new();
        _ = services.AddArchLucidAuthorizationPolicies();
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AuthorizationOptions> options = provider.GetRequiredService<IOptions<AuthorizationOptions>>();

        AuthorizationOptions authorizationOptions = options.Value;
        authorizationOptions.GetPolicy(ArchLucidPolicies.ReadAuthority).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.ExecuteAuthority).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.AdminAuthority).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.RequireAuditor).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.RequireReadOnly).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.CanCommitRuns).Should().NotBeNull();
    }
}
