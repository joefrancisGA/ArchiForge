using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Persistence.CustomerSuccess;

namespace ArchLucid.Persistence.Tests.CustomerSuccess;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class InMemoryTenantCustomerSuccessRepositoryTests
{
    [Fact]
    public async Task GetHealthScoreAsync_returns_null()
    {
        InMemoryTenantCustomerSuccessRepository sut = new();

        TenantHealthScoreRecord? row = await sut.GetHealthScoreAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        row.Should().BeNull();
    }

    [Fact]
    public async Task InsertProductFeedbackAsync_completes_without_throw()
    {
        InMemoryTenantCustomerSuccessRepository sut = new();

        Func<Task> act = async () => await sut.InsertProductFeedbackAsync(
            new ProductFeedbackSubmission
            {
                TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid(), Score = -1
            },
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RefreshAllTenantHealthScoresAsync_completes_without_throw()
    {
        InMemoryTenantCustomerSuccessRepository sut = new();

        Func<Task> act = async () => await sut.RefreshAllTenantHealthScoresAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
