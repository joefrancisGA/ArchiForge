using ArchLucid.Persistence.Tenancy;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Tenancy;

public sealed class SqlTenantHardPurgeServiceTests
{
    [SkippableFact]
    public void BuildPurgeSql_Returns_parameterized_delete_for_allowlisted_table()
    {
        string sql = SqlTenantHardPurgeService.BuildPurgeSql("dbo.UsageEvents");

        sql.Should()
            .Be("DELETE TOP (@Cap) FROM dbo.UsageEvents WHERE TenantId = @TenantId");
    }

    [SkippableFact]
    public void BuildPurgeSql_Throws_InvalidOperationException_when_table_not_allowlisted()
    {
        Action act = () => SqlTenantHardPurgeService.BuildPurgeSql("dbo.Injected; DROP TABLE dbo.Tenants;--");

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*not in the approved tenant-scoped purge list*");
    }

    [SkippableFact]
    public void BuildPurgeSql_Throws_ArgumentNullException_when_table_null()
    {
        Action act = () => SqlTenantHardPurgeService.BuildPurgeSql(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("table");
    }
}
