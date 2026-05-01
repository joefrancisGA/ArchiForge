using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Data.Repositories;

[Trait("Category", "Unit")]
public sealed class InMemoryGovernanceApprovalRequestRepositoryMaxRowsValidationTests
{
    [SkippableFact]
    public async Task GetPendingAsync_throws_when_maxRows_not_positive()
    {
        InMemoryGovernanceApprovalRequestRepository sut = new();

        Func<Task> act = async () => await sut.GetPendingAsync(0, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("maxRows");
    }

    [SkippableFact]
    public async Task GetRecentDecisionsAsync_throws_when_maxRows_not_positive()
    {
        InMemoryGovernanceApprovalRequestRepository sut = new();

        Func<Task> act = async () => await sut.GetRecentDecisionsAsync(-1, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("maxRows");
    }
}
