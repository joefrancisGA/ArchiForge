using ArchiForge.Persistence.RelationalRead;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Contract tests for <see cref="RelationalFirstRead"/> integration with <see cref="JsonFallbackPolicy"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RelationalFirstReadTests
{
    [Fact]
    public async Task ReadSliceAsync_RelationalRowsExist_CallsRelationalLoader()
    {
        bool relationalCalled = false;

        List<string> result = await RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 3,
            "Test.Slice",
            () => { relationalCalled = true; return Task.FromResult(new List<string> { "relational" }); },
            () => ["json-fallback"],
            () => [],
            policy: new JsonFallbackPolicy { AllowFallback = true });

        relationalCalled.Should().BeTrue();
        result.Should().Equal("relational");
    }

    [Fact]
    public async Task ReadSliceAsync_NoRows_PolicyAllows_CallsJsonFallback()
    {
        bool jsonCalled = false;

        List<string> result = await RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 0,
            "Test.Slice",
            () => Task.FromResult(new List<string> { "relational" }),
            () => { jsonCalled = true; return ["json-fallback"]; },
            () => [],
            policy: new JsonFallbackPolicy { AllowFallback = true });

        jsonCalled.Should().BeTrue();
        result.Should().Equal("json-fallback");
    }

    [Fact]
    public async Task ReadSliceAsync_NoRows_PolicyDenies_ReturnsEmptyDefault()
    {
        bool jsonCalled = false;

        List<string> result = await RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 0,
            "Test.Slice",
            () => Task.FromResult(new List<string> { "relational" }),
            () => { jsonCalled = true; return ["json-fallback"]; },
            () => [],
            policy: new JsonFallbackPolicy { AllowFallback = false });

        jsonCalled.Should().BeFalse();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadSliceAsync_NoRows_NullPolicy_FallsBackToJson()
    {
        List<string> result = await RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 0,
            "Test.Slice",
            () => Task.FromResult(new List<string> { "relational" }),
            () => ["json-fallback"],
            () => [],
            policy: null);

        result.Should().Equal("json-fallback");
    }

    [Fact]
    public async Task ReadSliceAsync_BackwardCompatOverload_AlwaysFallsBack()
    {
        List<string> result = await RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 0,
            () => Task.FromResult(new List<string> { "relational" }),
            () => ["json-fallback"]);

        result.Should().Equal("json-fallback");
    }
}
