using System.Text.Json;

using ArchLucid.Application.Scim.Patching;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Scim;

[Trait("Suite", "Core")]
public sealed class ScimPatchOpEvaluatorTests
{
    [SkippableFact]
    public void Replace_sets_value()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase) { ["userName"] = JsonDocument.Parse("\"old\"").RootElement };

        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"replace","path":"userName","value":"new"}]}""").RootElement;

        IReadOnlyDictionary<string, JsonElement> next = ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        next["userName"].GetString().Should().Be("new");
    }

    [SkippableFact]
    public void Add_inserts_value()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase);
        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"add","path":"active","value":true}]}""").RootElement;

        IReadOnlyDictionary<string, JsonElement> next = ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        next["active"].GetBoolean().Should().BeTrue();
    }

    [SkippableFact]
    public void Remove_deletes_key()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase) { ["displayName"] = JsonDocument.Parse("\"x\"").RootElement };

        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"remove","path":"displayName"}]}""").RootElement;

        IReadOnlyDictionary<string, JsonElement> next = ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        next.ContainsKey("displayName").Should().BeFalse();
    }

    [SkippableFact]
    public void Invalid_member_filter_value_throws_invalid_path()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase);
        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"replace","path":"members[value eq \"x\"]","value":[]}]}""").RootElement;

        Action act = () => ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        ScimPatchException ex = act.Should().Throw<ScimPatchException>().Which;
        ex.ScimType.Should().Be("invalidPath");
    }

    [SkippableFact]
    public void Valid_guid_member_path_on_user_throws_not_implemented()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase);
        Guid id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        JsonElement patch = JsonDocument.Parse(
            $$"""{"Operations":[{"op":"replace","path":"members[value eq \"{{id:D}}\"]","value":true}]}""").RootElement;

        Action act = () => ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        ScimPatchException ex = act.Should().Throw<ScimPatchException>().Which;
        ex.ScimType.Should().Be("notImplemented");
    }

    [SkippableFact]
    public void Missing_value_on_replace_throws()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase);
        JsonElement patch = JsonDocument.Parse(
            """{"Operations":[{"op":"replace","path":"userName"}]}""").RootElement;

        Action act = () => ScimPatchOpEvaluator.ApplyFlat(cur, patch);
        act.Should().Throw<ScimPatchException>().Which.ScimType.Should().Be("invalidValue");
    }

    [SkippableFact]
    public void Sequential_ops_apply_in_order()
    {
        Dictionary<string, JsonElement> cur = new(StringComparer.OrdinalIgnoreCase) { ["a"] = JsonDocument.Parse("1").RootElement };

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
