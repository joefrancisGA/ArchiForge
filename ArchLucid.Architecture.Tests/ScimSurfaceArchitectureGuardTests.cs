using FluentAssertions;

namespace ArchLucid.Architecture.Tests;

/// <summary>Source-level guardrails for the SCIM HTTP surface (no runtime dependency on <c>ArchLucid.Api</c> assembly).</summary>
[Trait("Suite", "Core")]
public sealed class ScimSurfaceArchitectureGuardTests
{
    private static string FindRepoRoot()
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            string sln = Path.Combine(d.FullName, "ArchLucid.sln");
            if (File.Exists(sln))
                return d.FullName;
        }

        throw new InvalidOperationException("ArchLucid.sln not found walking up from AppContext.BaseDirectory.");
    }

    [Fact]
    public void Scim_controllers_must_not_allow_anonymous()
    {
        string root = FindRepoRoot();
        string dir = Path.Combine(root, "ArchLucid.Api", "Controllers", "Scim");
        Directory.Exists(dir).Should().BeTrue();

        foreach (string path in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(path);
            text.Should().NotContain("[AllowAnonymous]", $"Remove anonymous access from {path}.");
        }
    }

    [Fact]
    public void AuthServiceCollectionExtensions_registers_ScimBearer_scheme()
    {
        string root = FindRepoRoot();
        string path = Path.Combine(root, "ArchLucid.Api", "Auth", "Services", "AuthServiceCollectionExtensions.cs");
        File.Exists(path).Should().BeTrue();
        string text = File.ReadAllText(path);
        text.Should().Contain("ScimBearerAuthenticationHandler");
        text.Should().Contain("ScimBearerDefaults.AuthenticationScheme");
    }
}
