using ArchLucid.Persistence.Data.Infrastructure;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SqlConnectionStringSecurityTests
{
    [Fact]
    public void EnsureSqlClientEncryptMandatory_throws_on_null_or_whitespace()
    {
        Action a1 = () => SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(null!);
        a1.Should().Throw<ArgumentException>().WithParameterName("connectionString");

        Action a2 = () => SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory("  ");
        a2.Should().Throw<ArgumentException>().WithParameterName("connectionString");
    }

    [Fact]
    public void EnsureSqlClientEncryptMandatory_sets_mandatory_encrypt()
    {
        const string input = "Server=localhost;Database=db1;Trusted_Connection=True;";

        string result = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(input);

        SqlConnectionStringBuilder roundTrip = new(result);
        roundTrip.Encrypt.Should().Be(SqlConnectionEncryptOption.Mandatory);
    }

    [Fact]
    public void EnsureSqlClientEncryptMandatory_is_idempotent()
    {
        const string input = "Server=localhost;Database=db1;Trusted_Connection=True;";

        string once = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(input);
        string twice = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(once);

        twice.Should().Be(once);
    }
}
