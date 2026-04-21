using ArchLucid.Application.Notifications.Email;

using FluentAssertions;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Host.Composition.Tests.ExecDigest;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ExecDigestUnsubscribeTokenFactoryTests
{
    [Fact]
    public void Round_trips_tenant_id()
    {
        ServiceCollection services = [];
        services.AddDataProtection();
        ServiceProvider sp = services.BuildServiceProvider();
        IDataProtectionProvider provider = sp.GetRequiredService<IDataProtectionProvider>();
        ExecDigestUnsubscribeTokenFactory factory = new(provider);

        Guid tenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        string token = factory.CreateToken(tenantId);

        bool ok = factory.TryParseTenant(token, out Guid parsed);

        ok.Should().BeTrue();
        parsed.Should().Be(tenantId);
    }
}
