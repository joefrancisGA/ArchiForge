using ArchLucid.Application.Scim.Patching;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Scim;

[Trait("Suite", "Core")]
public sealed class ScimPatchValuePathParserTests
{
    private static readonly Guid SampleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void ParseForGroup_flat_members_returns_flat_outcome()
    {
        ScimPatchPathParseOutcome o = ScimPatchValuePathParser.ParseForGroupMemberPath("members");
        ScimPatchFlatAttributePathOutcome flat = o.Should().BeOfType<ScimPatchFlatAttributePathOutcome>().Which;
        flat.AttributePath.Should().Be("members");
    }

    [Fact]
    public void ParseForGroup_filtered_value_eq_returns_user_and_null_sub_attribute()
    {
        string path = $"""members[value eq "{SampleId:D}"]""";
        ScimPatchPathParseOutcome o = ScimPatchValuePathParser.ParseForGroupMemberPath(path);
        ScimPatchMembersFilteredPathOutcome f = o.Should().BeOfType<ScimPatchMembersFilteredPathOutcome>().Which;
        f.ReferenceUserId.Should().Be(SampleId);
        f.SubAttribute.Should().BeNull();
    }

    [Fact]
    public void ParseForGroup_filtered_value_eq_dot_active_sets_sub_attribute()
    {
        string path = $"""members[value eq "{SampleId:D}"].active""";
        ScimPatchPathParseOutcome o = ScimPatchValuePathParser.ParseForGroupMemberPath(path);
        ScimPatchMembersFilteredPathOutcome f = o.Should().BeOfType<ScimPatchMembersFilteredPathOutcome>().Which;
        f.SubAttribute.Should().Be("active");
    }

    [Fact]
    public void ParseForGroup_unclosed_bracket_invalid_path_outcome()
    {
        ScimPatchPathParseOutcome o =
            ScimPatchValuePathParser.ParseForGroupMemberPath($"members[value eq \"{SampleId:D}\"");

        ScimPatchPathInvalidOutcome invalid = o.Should().BeOfType<ScimPatchPathInvalidOutcome>().Which;
        invalid.Detail.Should().Contain("Unclosed '['");
    }

    [Fact]
    public void ParseForGroup_members_value_ne_returns_not_implemented_outcome()
    {
        string path = $"""members[value ne "{SampleId:D}"]""";
        ScimPatchPathParseOutcome o = ScimPatchValuePathParser.ParseForGroupMemberPath(path);

        ScimPatchPathNotImplementedOutcome ni = o.Should().BeOfType<ScimPatchPathNotImplementedOutcome>().Which;
        ni.Detail.Should().Contain("Only 'value eq");
    }

    [Fact]
    public void ParseForGroup_other_attribute_filtered_returns_not_implemented_when_eq_shape()
    {
        string path = $"""roles[value eq "{SampleId:D}"]""";

        ScimPatchPathParseOutcome o = ScimPatchValuePathParser.ParseForGroupMemberPath(path);

        ScimPatchPathNotImplementedOutcome ni = o.Should().BeOfType<ScimPatchPathNotImplementedOutcome>().Which;
        ni.Detail.Should().Contain("roles");
    }

    [Fact]
    public void ParseForUser_filtered_members_throws_ScimPatchException_not_implemented()
    {
        string path = $"""members[value eq "{SampleId:D}"]""";

        Action act = () => ScimPatchValuePathParser.ParseForUserFlatPatchPath(path);

        ScimPatchException ex = act.Should().Throw<ScimPatchException>().Which;
        ex.ScimType.Should().Be("notImplemented");
    }

    [Fact]
    public void ParseForUser_other_attribute_value_eq_guid_not_implemented()
    {
        string path = """emails[value eq "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"]""";

        Action act = () => ScimPatchValuePathParser.ParseForUserFlatPatchPath(path);

        ScimPatchException ex = act.Should().Throw<ScimPatchException>().Which;
        ex.ScimType.Should().Be("notImplemented");
    }

    [Fact]
    public void ParseForUser_invalid_guid_in_filter_invalid_path()
    {
        Action act = () => ScimPatchValuePathParser.ParseForUserFlatPatchPath(@"members[value eq ""x""]");

        ScimPatchException ex = act.Should().Throw<ScimPatchException>().Which;
        ex.ScimType.Should().Be("invalidPath");
    }
}
