using System.Text.Json;

using ArchLucid.Application.Scim;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scim.Models;
using ArchLucid.Persistence.Scim;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Scim;

[Trait("Suite", "Core")]
public sealed class ScimGroupServicePatchMembersTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid User1 = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-aaaaaaaaaaaa");
    private static readonly Guid User2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid User3 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Remove_member_by_value_path_drops_only_matching_user()
    {
        (ScimGroupService sut, InMemoryScimGroupRepository repo, Guid groupId) = await CreateSutWithGroupAndMembersAsync(
            [User1, User2]);

        JsonElement patch = JsonDocument.Parse(
            $$"""
            {"Operations":[{"op":"remove","path":"members[value eq \"{{User1:D}}\"]"}]}
            """).RootElement;

        await sut.PatchMembersAsync(TenantId, groupId, patch, CancellationToken.None);

        IReadOnlyList<Guid> after = await repo.ListMemberUserIdsAsync(TenantId, groupId, CancellationToken.None);
        after.Should().Equal(User2);
    }

    [Fact]
    public async Task Replace_members_dot_active_false_removes_member()
    {
        (ScimGroupService sut, InMemoryScimGroupRepository repo, Guid groupId) =
            await CreateSutWithGroupAndMembersAsync([User1]);

        JsonElement patch = JsonDocument.Parse(
            $$"""
            {"Operations":[{"op":"replace","path":"members[value eq \"{{User1:D}}\"].active","value":false}]}
            """).RootElement;

        await sut.PatchMembersAsync(TenantId, groupId, patch, CancellationToken.None);

        IReadOnlyList<Guid> after = await repo.ListMemberUserIdsAsync(TenantId, groupId, CancellationToken.None);
        after.Should().BeEmpty();
    }

    [Fact]
    public async Task Bulk_add_keeps_prior_members()
    {
        (ScimGroupService sut, InMemoryScimGroupRepository repo, Guid groupId) =
            await CreateSutWithGroupAndMembersAsync([User1]);

        JsonElement patch = JsonDocument.Parse(
            $$"""
            {"Operations":[{"op":"add","path":"members","value":[{"value":"{{User2:D}}"},{"value":"{{User3:D}}"}]}]}
            """).RootElement;

        await sut.PatchMembersAsync(TenantId, groupId, patch, CancellationToken.None);

        IReadOnlyList<Guid> after = await repo.ListMemberUserIdsAsync(TenantId, groupId, CancellationToken.None);
        after.Should().Equal(User1, User2, User3);
    }

    [Fact]
    public async Task Value_ne_operator_throws_not_implemented_Scim_parse_exception()
    {
        (ScimGroupService sut, _, Guid groupId) =
            await CreateSutWithGroupAndMembersAsync([User1]);

        JsonElement patch = JsonDocument.Parse(
            $$"""
            {"Operations":[{"op":"remove","path":"members[value ne \"{{User1:D}}\"]"}]}
            """).RootElement;

        Func<Task> act = () => sut.PatchMembersAsync(TenantId, groupId, patch, CancellationToken.None);
        ScimUserResourceParseException ex = (await act.Should().ThrowAsync<ScimUserResourceParseException>()).Which;
        ex.ScimType.Should().Be("notImplemented");
    }

    [Fact]
    public async Task Bulk_replace_replaces_membership_exactly()
    {
        (ScimGroupService sut, InMemoryScimGroupRepository repo, Guid groupId) =
            await CreateSutWithGroupAndMembersAsync([User1, User2]);

        JsonElement patch = JsonDocument.Parse(
            $$"""
            {"Operations":[{"op":"replace","path":"members","value":[{"value":"{{User2:D}}"}]}]}
            """).RootElement;

        await sut.PatchMembersAsync(TenantId, groupId, patch, CancellationToken.None);

        IReadOnlyList<Guid> after = await repo.ListMemberUserIdsAsync(TenantId, groupId, CancellationToken.None);
        after.Should().Equal(User2);
    }

    private static async Task<(ScimGroupService Sut, InMemoryScimGroupRepository Repo, Guid GroupId)>
        CreateSutWithGroupAndMembersAsync(IReadOnlyList<Guid> initialMembers)
    {
        InMemoryScimGroupRepository repo = new();
        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        ScimGroupService sut = new(repo, audit.Object);
        ScimGroupRecord g = await repo.InsertAsync(TenantId, "g-ext", "G", CancellationToken.None);
        await repo.SetMembersAsync(TenantId, g.Id, initialMembers, CancellationToken.None);

        return (sut, repo, g.Id);
    }
}
