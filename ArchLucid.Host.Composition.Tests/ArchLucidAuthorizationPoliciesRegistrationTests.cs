using ArchLucid.Core.Authorization;
using ArchLucid.Host.Core.Authorization;
using ArchLucid.Host.Core.Startup;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Composition.Tests;

/// <summary>
///     Ensures <see cref="ArchLucidAuthorizationPoliciesExtensions.AddArchLucidAuthorizationPolicies" /> registers
///     expected policies.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArchLucidAuthorizationPoliciesRegistrationTests
{
    [Fact]
    public void AddArchLucidAuthorizationPolicies_registers_core_role_and_auditor_policies()
    {
        AuthorizationOptions authorizationOptions = BuildAuthorizationOptions();

        authorizationOptions.GetPolicy(ArchLucidPolicies.ReadAuthority).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.ExecuteAuthority).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.AdminAuthority).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.RequireAuditor).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.RequireReadOnly).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.RequireOperator).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.RequireAdmin).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.CanCommitRuns).Should().NotBeNull();

        authorizationOptions.GetPolicy(ArchLucidPolicies.ExecuteAuthority)!.Requirements
            .OfType<TrialActiveRequirement>()
            .Should()
            .ContainSingle();

        authorizationOptions.GetPolicy(ArchLucidPolicies.AdminAuthority)!.Requirements
            .OfType<TrialActiveRequirement>()
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void AddArchLucidAuthorizationPolicies_registers_permission_policies()
    {
        AuthorizationOptions authorizationOptions = BuildAuthorizationOptions();

        authorizationOptions.GetPolicy(ArchLucidPolicies.CanExportConsultingDocx).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.CanReplayComparisons).Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.CanViewReplayDiagnostics).Should().NotBeNull();
        authorizationOptions.GetPolicy("CanSeedResults").Should().NotBeNull();
        authorizationOptions.GetPolicy(ArchLucidPolicies.ScimWrite).Should().NotBeNull();
    }

    [Fact]
    public void AddArchLucidAuthorizationPolicies_sets_fallback_requiring_authenticated_user()
    {
        AuthorizationOptions authorizationOptions = BuildAuthorizationOptions();

        authorizationOptions.FallbackPolicy.Should().NotBeNull();
        authorizationOptions.FallbackPolicy!.Requirements
            .OfType<DenyAnonymousAuthorizationRequirement>()
            .Should()
            .ContainSingle();
    }

    private static AuthorizationOptions BuildAuthorizationOptions()
    {
        ServiceCollection services = [];
        _ = services.AddArchLucidAuthorizationPolicies();
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AuthorizationOptions> options = provider.GetRequiredService<IOptions<AuthorizationOptions>>();

        return options.Value;
    }
}
