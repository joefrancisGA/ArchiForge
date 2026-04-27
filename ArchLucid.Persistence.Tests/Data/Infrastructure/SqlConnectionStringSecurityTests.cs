using ArchLucid.Persistence.Data.Infrastructure;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

[Trait("Category", "Unit")]
public sealed class SqlConnectionStringSecurityTests
{
    [Fact]
    public void EnsureSqlClientEncryptMandatory_TrimsAndSetsEncryptMandatory()
    {
        string input = " Server=localhost; Database=Db; Integrated Security=true; TrustServerCertificate=true; ";

        string actual = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(input);

        SqlConnectionStringBuilder b = new(actual);
        b.Encrypt.Should().Be(SqlConnectionEncryptOption.Mandatory);
        b.DataSource.Should().Be("localhost");
    }

    [Fact]
    public void EnsureSqlClientEncryptMandatory_Throws_WhenNullOrWhiteSpace()
    {
        Action actNull = () => SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(null!);
        Action actEmpty = () => SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory("   ");

        actNull.Should().Throw<ArgumentException>().WithParameterName("connectionString");
        actEmpty.Should().Throw<ArgumentException>().WithParameterName("connectionString");
    }
}
