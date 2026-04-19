using System.Text.RegularExpressions;

using FluentAssertions;

namespace ArchLucid.Architecture.Tests;

/// <summary>
/// Lightweight DDL guard: authority tables include tenant scope columns aligned with RLS predicates
/// (see <c>docs/DATA_MODEL.md</c>, <c>docs/TENANT_SCOPED_TABLES_INVENTORY.md</c>).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TenantScopedTableDdlTests
{
    public static TheoryData<string, string> TenantScopedTablesWithGuidProjectColumn =>
        new()
        {
            { "AuditEvents", "ProjectId" },
            { "IntegrationEventOutbox", "ProjectId" },
            { "GoldenManifests", "ProjectId" },
        };

    [Fact]
    public void ArchLucid_sql_Runs_create_table_includes_TenantId_WorkspaceId_ProjectId_and_ScopeProjectId_columns()
    {
        string sql = File.ReadAllText(ResolveArchLucidSqlPath());
        string header = ExtractCreateTableHeader(sql, "Runs");

        header.Should().MatchRegex(@"(?<!\w)TenantId(?!\w)", because: "Runs is tenant-scoped");
        header.Should().MatchRegex(@"(?<!\w)WorkspaceId(?!\w)", because: "Runs is workspace-scoped");
        header.Should().MatchRegex(@"(?<!\w)ProjectId(?!\w)", because: "Runs carries project key (NVARCHAR line-of-business id)");
        header.Should().MatchRegex(@"(?<!\w)ScopeProjectId(?!\w)", because: "Runs carries RLS scope surrogate key");
    }

    [Theory]
    [MemberData(nameof(TenantScopedTablesWithGuidProjectColumn))]
    public void ArchLucid_sql_create_table_includes_tenant_workspace_and_project_scope_columns(
        string tableName,
        string projectScopeColumnName)
    {
        string sql = File.ReadAllText(ResolveArchLucidSqlPath());
        string header = ExtractCreateTableHeader(sql, tableName);

        header.Should().MatchRegex(@"(?<!\w)TenantId(?!\w)");
        header.Should().MatchRegex(@"(?<!\w)WorkspaceId(?!\w)");
        header.Should().MatchRegex($@"(?<!\w){Regex.Escape(projectScopeColumnName)}(?!\w)");
    }

    private static string ExtractCreateTableHeader(string sql, string tableName)
    {
        string escaped = Regex.Escape(tableName);
        Match m = Regex.Match(
            sql,
            $@"CREATE\s+TABLE\s+(?:\[dbo\]\.\[{escaped}\]|dbo\.{escaped})\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        m.Success.Should().BeTrue($"ArchLucid.sql should define dbo.{tableName}");

        int start = m.Index;
        int depth = 0;
        int end = start;

        for (int i = start; i < sql.Length; i++)
        {
            char c = sql[i];

            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;

                if (depth == 0)
                {
                    end = i + 1;
                    break;
                }
            }
        }

        depth.Should().Be(0, $"unbalanced parentheses in dbo.{tableName} CREATE TABLE");

        return sql[start..end];
    }

    private static string ResolveArchLucidSqlPath()
    {
        string[] seeds =
        [
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
        ];

        foreach (string seed in seeds)
        {
            string dir = Path.GetFullPath(seed);

            for (int depth = 0; depth < 16 && !string.IsNullOrEmpty(dir); depth++)
            {
                string candidate = Path.Combine(dir, "ArchLucid.Persistence", "Scripts", "ArchLucid.sql");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                string? parent = Path.GetDirectoryName(dir);

                if (string.IsNullOrEmpty(parent) || string.Equals(parent, dir, StringComparison.Ordinal))
                {
                    break;
                }

                dir = parent;
            }
        }

        throw new InvalidOperationException(
            "Could not locate ArchLucid.Persistence/Scripts/ArchLucid.sql. Run tests from repo root or ensure the file is present.");
    }
}
