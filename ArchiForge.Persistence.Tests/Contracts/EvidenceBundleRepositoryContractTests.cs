using ArchiForge.Contracts.Agents;
using ArchiForge.Data.Repositories;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IEvidenceBundleRepository"/>.
/// </summary>
public abstract class EvidenceBundleRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IEvidenceBundleRepository CreateRepository();

    [SkippableFact]
    public async Task Create_then_GetById_round_trips()
    {
        SkipIfSqlServerUnavailable();
        IEvidenceBundleRepository repo = CreateRepository();
        EvidenceBundle bundle = new()
        {
            EvidenceBundleId = "eb-" + Guid.NewGuid().ToString("N"),
            RequestDescription = "rd",
        };

        await repo.CreateAsync(bundle, CancellationToken.None);

        EvidenceBundle? loaded = await repo.GetByIdAsync(bundle.EvidenceBundleId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.EvidenceBundleId.Should().Be(bundle.EvidenceBundleId);
        loaded.RequestDescription.Should().Be("rd");
    }
}
