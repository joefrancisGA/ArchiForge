using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IArchitectureRequestRepository" />.
/// </summary>
public abstract class ArchitectureRequestRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IArchitectureRequestRepository CreateRepository();

    [SkippableFact]
    public async Task Create_then_GetById_round_trips()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureRequestRepository repo = CreateRepository();
        string requestId = "arrq-" + Guid.NewGuid().ToString("N");

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "SysX",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Description = "desc"
        };

        await repo.CreateAsync(request, CancellationToken.None);

        ArchitectureRequest? loaded = await repo.GetByIdAsync(requestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.RequestId.Should().Be(requestId);
        loaded.SystemName.Should().Be("SysX");
    }
}
