using ArchLucid.Application.Scim.Filtering;

using ArchLucid.Core.Scim.Filtering;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Scim;

[Trait("Suite", "Core")]
public sealed class ScimFilterParserTests
{
    [Fact]
    public void Parse_null_or_whitespace_returns_null()
    {
        ScimFilterParser.Parse(null).Should().BeNull();
        ScimFilterParser.Parse("   ").Should().BeNull();
    }

    [Fact]
    public void Parse_simple_eq()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(@"userName eq ""alice""");
        ScimComparisonNode c = n.Should().BeOfType<ScimComparisonNode>().Subject;
        c.AttributePath.Should().Be("userName");
        c.Operator.Should().Be("eq");
        c.Value.Should().Be("alice");
    }

    [Fact]
    public void Parse_active_eq_true()
    {
        ScimFilterNode? n = ScimFilterParser.Parse("active eq true");
        ScimComparisonNode c = n.Should().BeOfType<ScimComparisonNode>().Subject;
        c.AttributePath.Should().Be("active");
        c.Value.Should().Be("true");
    }

    [Fact]
    public void Parse_gt_and_le()
    {
        ScimFilterParser.Parse(@"displayName gt ""b""")!.Should().BeOfType<ScimComparisonNode>().Which.Operator.Should().Be("gt");
        ScimFilterParser.Parse(@"externalId le ""z""")!.Should().BeOfType<ScimComparisonNode>().Which.Operator.Should().Be("le");
    }

    [Fact]
    public void Parse_co_sw_ew()
    {
        ScimFilterParser.Parse(@"userName co ""corp""")!.Should().BeOfType<ScimComparisonNode>().Which.Operator.Should().Be("co");
        ScimFilterParser.Parse(@"userName sw ""a""")!.Should().BeOfType<ScimComparisonNode>().Which.Operator.Should().Be("sw");
        ScimFilterParser.Parse(@"userName ew ""z""")!.Should().BeOfType<ScimComparisonNode>().Which.Operator.Should().Be("ew");
    }

    [Fact]
    public void Parse_pr()
    {
        ScimFilterNode? n = ScimFilterParser.Parse("displayName pr");
        ScimPresentNode p = n.Should().BeOfType<ScimPresentNode>().Subject;
        p.AttributePath.Should().Be("displayName");
    }

    [Fact]
    public void Parse_and_left_associative()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(@"userName eq ""a"" and active eq true");
        ScimAndNode a = n.Should().BeOfType<ScimAndNode>().Subject;
        a.Left.Should().BeOfType<ScimComparisonNode>();
        a.Right.Should().BeOfType<ScimComparisonNode>();
    }

    [Fact]
    public void Parse_or()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(@"userName eq ""a"" or userName eq ""b""");
        ScimOrNode o = n.Should().BeOfType<ScimOrNode>().Subject;
        o.Left.Should().BeOfType<ScimComparisonNode>();
        o.Right.Should().BeOfType<ScimComparisonNode>();
    }

    [Fact]
    public void Parse_not()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(@"not (userName eq ""x"")");
        ScimNotNode nn = n.Should().BeOfType<ScimNotNode>().Subject;
        nn.Inner.Should().BeOfType<ScimComparisonNode>();
    }

    [Fact]
    public void Parse_nested_and_or()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(
            @"(userName eq ""a"" or userName eq ""b"") and active eq true");
        ScimAndNode root = n.Should().BeOfType<ScimAndNode>().Subject;
        root.Left.Should().BeOfType<ScimOrNode>();
        root.Right.Should().BeOfType<ScimComparisonNode>();
    }

    [Fact]
    public void Parse_triple_and()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(
            @"((userName eq ""a"" and externalId eq ""b"") and displayName eq ""c"")");
        ScimAndNode outer = n.Should().BeOfType<ScimAndNode>().Subject;
        outer.Left.Should().BeOfType<ScimAndNode>();
        outer.Right.Should().BeOfType<ScimComparisonNode>();
    }

    [Fact]
    public void Parse_not_and_combo()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(@"(not (active eq ""false"")) and userName pr");
        ScimAndNode a = n.Should().BeOfType<ScimAndNode>().Subject;
        a.Left.Should().BeOfType<ScimNotNode>();
        a.Right.Should().BeOfType<ScimPresentNode>();
    }

    [Fact]
    public void Parse_or_with_not_branch()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(@"userName eq ""a"" or (not (externalId eq ""e""))");
        ScimOrNode o = n.Should().BeOfType<ScimOrNode>().Subject;
        o.Right.Should().BeOfType<ScimNotNode>();
    }

    [Fact]
    public void Parse_deep_parentheses()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(@"((userName eq ""z""))");
        n.Should().BeOfType<ScimComparisonNode>();
    }

    [Fact]
    public void Parse_ne()
    {
        ScimFilterParser.Parse(@"userName ne ""blocked""")!.Should().BeOfType<ScimComparisonNode>().Which.Operator.Should().Be("ne");
    }

    [Fact]
    public void Parse_externalId_eq_quoted_with_spaces()
    {
        ScimFilterNode? n = ScimFilterParser.Parse(@"externalId eq ""archlucid:admins""");
        ScimComparisonNode c = n.Should().BeOfType<ScimComparisonNode>().Subject;
        c.Value.Should().Be("archlucid:admins");
    }

    [Fact]
    public void Parse_trailing_input_throws()
    {
        Action act = () => ScimFilterParser.Parse(@"userName eq ""a"" junk");
        act.Should().Throw<ScimFilterParseException>();
    }
}
