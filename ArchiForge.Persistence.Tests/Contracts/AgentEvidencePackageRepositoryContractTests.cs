using ArchiForge.Contracts.Agents;
using ArchiForge.Data.Repositories;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IAgentEvidencePackageRepository"/>.
/// </summary>
public abstract class AgentEvidencePackageRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IAgentEvidencePackageRepository CreateRepository();

    protected virtual Task PrepareRequestAndRunAsync(string requestId, string runId, CancellationToken ct)
    {
        _ = requestId;
        _ = runId;
        _ = ct;

        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Create_then_GetByRunId_and_GetById_round_trip()
    {
        SkipIfSqlServerUnavailable();
        IAgentEvidencePackageRepository repo = CreateRepository();
        string requestId = "aep-req-" + Guid.NewGuid().ToString("N");
        string runId = Guid.NewGuid().ToString("N");

        await PrepareRequestAndRunAsync(requestId, runId, CancellationToken.None);

        AgentEvidencePackage package = new()
        {
            EvidencePackageId = "pkg-" + Guid.NewGuid().ToString("N"),
            RunId = runId,
            RequestId = requestId,
            SystemName = "Sys",
            Environment = "prod",
            CloudProvider = "Azure",
            Request = new RequestEvidence { Description = "d" },
        };

        await repo.CreateAsync(package, CancellationToken.None);

        AgentEvidencePackage? byRun = await repo.GetByRunIdAsync(runId, CancellationToken.None);
        AgentEvidencePackage? byId = await repo.GetByIdAsync(package.EvidencePackageId, CancellationToken.None);

        byRun.Should().NotBeNull();
        byRun!.EvidencePackageId.Should().Be(package.EvidencePackageId);
        byId.Should().NotBeNull();
        byId!.RunId.Should().Be(runId);
    }
}
