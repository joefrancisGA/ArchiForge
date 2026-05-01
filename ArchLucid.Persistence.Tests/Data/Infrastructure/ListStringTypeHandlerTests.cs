using ArchLucid.Persistence.Data.Infrastructure;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

[Trait("Category", "Unit")]
public sealed class ListStringTypeHandlerTests
{
    private readonly ListStringTypeHandler _sut = new();

    [SkippableFact]
    public void Parse_ReturnsEmpty_ForNullDbNullOrBlank()
    {
        _sut.Parse(null!).Should().BeEmpty();
        _sut.Parse(DBNull.Value).Should().BeEmpty();
        _sut.Parse("  ").Should().BeEmpty();
    }

    [SkippableFact]
    public void Parse_ReturnsList_ForValidJson()
    {
        IReadOnlyList<string> r = _sut.Parse("""["a","b"]""");

        r.Should().Equal("a", "b");
    }

    [SkippableFact]
    public void Parse_ReturnsEmpty_OnInvalidJson()
    {
        IReadOnlyList<string> r = _sut.Parse("not json");

        r.Should().BeEmpty();
    }

    [SkippableFact]
    public void SetValue_SerializesOrDbNull()
    {
        SqlParameter p = new();

        _sut.SetValue(p, null);
        p.Value.Should().Be(DBNull.Value);

        _sut.SetValue(p, []);
        p.Value.Should().Be(DBNull.Value);

        _sut.SetValue(p, ["x"]);
        p.Value.ToString().Should().Contain("x");
    }

    [SkippableFact]
    public void Register_IsIdempotent()
    {
        ListStringTypeHandler.Register();
        ListStringTypeHandler.Register();
    }
}
