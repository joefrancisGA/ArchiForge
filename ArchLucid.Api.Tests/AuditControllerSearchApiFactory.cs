using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Audit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>API factory with a replaceable <see cref="IAuditRepository"/> mock for audit controller tests.</summary>
public sealed class AuditControllerSearchApiFactory : ArchLucidApiFactory
{
    public Mock<IAuditRepository> AuditRepositoryMock { get; } = new();

    public AuditControllerSearchApiFactory()
    {
        AuditRepositoryMock
            .Setup(r => r.GetFilteredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuditRepository>();
            services.AddScoped(_ => AuditRepositoryMock.Object);
        });
    }
}
