namespace ArchiForge.Api.Client.Tests;

[Trait("Suite", "Core")]
public sealed class ArchiForgeApiClientPackageTests
{
    [Fact]
    public void Package_marker_type_is_public_for_discovery()
    {
        Type t = typeof(ArchiForgeApiClientPackage);

        Assert.True(t.IsPublic);
        Assert.True(t.IsAbstract && t.IsSealed);
    }
}
