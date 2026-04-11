using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Persistence.Data.Repositories;

#pragma warning disable CS0618 // RunsAuthorityConvergence: tracked for migration by 2026-09-30 — contract tests for legacy InMemoryArchitectureRunRepository.

namespace ArchLucid.Persistence.Tests.Contracts;

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

#pragma warning restore CS0618
