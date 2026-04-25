using System.Text.Json;

using ArchLucid.Application.Scim.Patching;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Scim;

[Trait("Suite", "Core")]
public sealed class ScimPatchOpEvaluatorTests
{
    [Fact]
    public void Replace_sets_value()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase)
        {
            ["userName"] = JsonDocument.Parse("\"old\"").RootElement
        };

        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"replace","path":"userName","value":"new"}]}""").RootElement;

        IReadOnlyDictionary<string, JsonElement> next = ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        next["userName"].GetString().Should().Be("new");
    }

    [Fact]
    public void Add_inserts_value()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase);
        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"add","path":"active","value":true}]}""").RootElement;

        IReadOnlyDictionary<string, JsonElement> next = ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        next["active"].GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void Remove_deletes_key()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase)
        {
            ["displayName"] = JsonDocument.Parse("\"x\"").RootElement
        };

        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"remove","path":"displayName"}]}""").RootElement;

        IReadOnlyDictionary<string, JsonElement> next = ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        next.ContainsKey("displayName").Should().BeFalse();
    }

    [Fact]
    public void Complex_path_throws_invalidPath()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase);
        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"replace","path":"members[value eq \"x\"]","value":[]}]}""").RootElement;

        Action act = () => ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        ScimPatchException ex = act.Should().Throw<ScimPatchException>().Which;
        ex.ScimType.Should().Be("invalidPath");
    }

    [Fact]
    public void Missing_value_on_replace_throws()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase);
        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"replace","path":"userName"}]}""").RootElement;

        Action act = () => ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        act.Should().Throw<ScimPatchException>().Which.ScimType.Should().Be("invalidValue");
    }

    [Fact]
    public void Sequential_ops_apply_in_order()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = JsonDocument.Parse("1").RootElement
        };

        JsonElement patch = JsonDocument.Parse(
            """
            {"Operations":[
              {"op":"replace","path":"a","value":2},
              {"op":"add","path":"b","value":"x"},
              {"op":"remove","path":"a"}
            ]}
            """).RootElement;

        IReadOnlyDictionary<string, JsonElement> next = ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        next.ContainsKey("a").Should().BeFalse();
        next["b"].GetString().Should().Be("x");
    }
}
