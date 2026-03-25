using System.Data;
using System.Text.Json;

using Dapper;

namespace ArchiForge.Data.Infrastructure;

/// <summary>Dapper type handler for List&lt;string&gt; stored as JSON in NVARCHAR columns.</summary>
public sealed class ListStringTypeHandler : SqlMapper.TypeHandler<List<string>>
{
    private static bool _registered;

    public override List<string> Parse(object value)
    {
        if (value is null or DBNull || value is not string s || string.IsNullOrWhiteSpace(s))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(s) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public override void SetValue(IDbDataParameter parameter, List<string>? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value == null || value.Count == 0
            ? DBNull.Value
            : JsonSerializer.Serialize(value);
    }

    public static void Register()
    {
        if (_registered)
            return;
        SqlMapper.AddTypeHandler(new ListStringTypeHandler());
        _registered = true;
    }
}
