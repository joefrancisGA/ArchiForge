using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryArchitectureRunRepositoryContractTests : ArchitectureRunRepositoryContractTests
{
    private readonly InMemoryArchitectureRequestRepository _requests = new();

    protected override IArchitectureRunRepository CreateRepository()
    {
        return new InMemoryArchitectureRunRepository(_requests);
    }

    protected override async Task PrepareRequestRowAsync(string requestId, string systemName, CancellationToken ct)
    {
        await _requests.CreateAsync(
            new ArchitectureRequest
            {
                RequestId = requestId,
                SystemName = systemName,
                Environment = "prod",
                CloudProvider = CloudProvider.Azure,
                Description = "d",
            },
            ct);
    }
}
