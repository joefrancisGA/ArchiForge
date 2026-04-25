using ArchLucid.Application.Scim.RoleMapping;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Tests.Scim;

[Trait("Suite", "Core")]
public sealed class GroupToRoleMapperTests
{
    [Theory]
    [InlineData("archlucid:admins", "x", ArchLucidRoles.Admin)]
    [InlineData("x", "archlucid:admins", ArchLucidRoles.Admin)]
    [InlineData("archlucid:operators", "", ArchLucidRoles.Operator)]
    [InlineData("", "archlucid:auditors", ArchLucidRoles.Auditor)]
    [InlineData("archlucid:readers", "archlucid:readers", ArchLucidRoles.Reader)]
    public void Default_mappings_resolve(string externalId, string display, string expected)
    {
        GroupToRoleMapper sut = new(Options.Create(new ScimOptions()));
        sut.TryMapGroupToRole(display, externalId).Should().Be(expected);
    }

    [Fact]
    public void Override_dictionary_wins_before_defaults()
    {
        ScimOptions opt = new()
        {
            GroupRoleMappingOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CustomAdmins"] = ArchLucidRoles.Operator
            }
        };

        GroupToRoleMapper sut = new(Options.Create(opt));
        sut.TryMapGroupToRole("CustomAdmins", "ignored").Should().Be(ArchLucidRoles.Operator);
    }

    [Fact]
    public void Unknown_group_returns_null()
    {
        GroupToRoleMapper sut = new(Options.Create(new ScimOptions()));
        sut.TryMapGroupToRole("other", "other").Should().BeNull();
    }
}
