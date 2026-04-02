using System.Data;
using System.Text.Json;

using ArchiForge.Data.Infrastructure;

using FluentAssertions;

namespace ArchiForge.Data.Tests.Infrastructure;

[Trait("Category", "Unit")]
public sealed class ListStringTypeHandlerTests
{
#pragma warning disable CS8766, CS8767 // Test fake: IDataParameter nullability vs concrete storage
    private sealed class FakeDbParameter : IDbDataParameter
    {
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable => false;
        public string ParameterName { get; set; } = "";
        public string SourceColumn { get; set; } = "";
        public DataRowVersion SourceVersion { get; set; }
        public object? Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }

#pragma warning restore CS8766, CS8767

    private readonly ListStringTypeHandler _handler = new();

    [Fact]
    public void Parse_Null_ReturnsEmptyList()
    {
        List<string> result = _handler.Parse(null!);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Parse_DbNull_ReturnsEmptyList()
    {
        List<string> result = _handler.Parse(DBNull.Value);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Parse_NonString_ReturnsEmptyList()
    {
        List<string> result = _handler.Parse(42);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyList()
    {
        List<string> result = _handler.Parse("");

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Parse_Whitespace_ReturnsEmptyList()
    {
        List<string> result = _handler.Parse("   ");

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Parse_ValidJson_ReturnsDeserializedList()
    {
        List<string> result = _handler.Parse("[\"a\",\"b\"]");

        result.Should().Equal("a", "b");
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsEmptyList()
    {
        List<string> result = _handler.Parse("not json");

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void SetValue_NullList_SetsDbNull()
    {
        FakeDbParameter parameter = new();

        _handler.SetValue(parameter, null);

        parameter.Value.Should().Be(DBNull.Value);
        parameter.DbType.Should().Be(DbType.String);
    }

    [Fact]
    public void SetValue_EmptyList_SetsDbNull()
    {
        FakeDbParameter parameter = new();

        _handler.SetValue(parameter, []);

        parameter.Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void SetValue_PopulatedList_SerializesJson()
    {
        FakeDbParameter parameter = new();
        List<string> list = ["x", "y"];

        _handler.SetValue(parameter, list);

        parameter.Value.Should().BeOfType<string>();
        string json = (string)parameter.Value;
        List<string>? roundTrip = JsonSerializer.Deserialize<List<string>>(json);
        roundTrip.Should().NotBeNull().And.Equal("x", "y");
    }

    [Fact]
    public void Register_CalledTwice_DoesNotThrow()
    {
        Action act = () =>
        {
            ListStringTypeHandler.Register();
            ListStringTypeHandler.Register();
        };

        act.Should().NotThrow();
    }
}
