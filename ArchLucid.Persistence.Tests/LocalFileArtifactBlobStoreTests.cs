using ArchLucid.Core.Scoping;

using Moq;

namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class LocalFileArtifactBlobStoreTests
{
    [Fact]
    public async Task Write_then_Read_round_trips_utf8_content()
    {
        string root = Path.Combine(Path.GetTempPath(), "archlucid-blob-test-" + Guid.NewGuid().ToString("N"));
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(static m => m.GetCurrentScope()).Returns(
            new ScopeContext
            {
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject
            });
        LocalFileArtifactBlobStore store = new(root, scope.Object);

        try
        {
            string uri = await store.WriteAsync("c1", "a/b.json", """{"x":1}""", CancellationToken.None);
            string? read = await store.ReadAsync(uri, CancellationToken.None);
            read.Should().Be("""{"x":1}""");
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public async Task ReadAsync_when_scope_tenant_mismatch_throws()
    {
        string root = Path.Combine(Path.GetTempPath(),
            "archlucid-blob-tenant-mismatch-" + Guid.NewGuid().ToString("N"));
        Guid writerTenant = Guid.Parse("10101010-1010-1010-1010-101010101010");
        Mock<IScopeContextProvider> scope = new();
        scope.SetupSequence(static m => m.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = writerTenant,
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    ProjectId = ScopeIds.DefaultProject
                })
            .Returns(
                new ScopeContext
                {
                    TenantId = Guid.Parse("20202020-2020-2020-2020-202020202020"),
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    ProjectId = ScopeIds.DefaultProject
                });
        LocalFileArtifactBlobStore store = new(root, scope.Object);

        try
        {
            string uri = await store.WriteAsync("c1", "a.json", "x", CancellationToken.None);
            Func<Task> act = async () => await store.ReadAsync(uri, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch (IOException)
            {
            }
        }
    }
}
