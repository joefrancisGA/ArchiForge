using ArchiForge.Cli.Support;

using FluentAssertions;

namespace ArchiForge.Cli.Tests;

/// <summary>
/// Support bundle environment snapshot: secret names and URL redaction (56R).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class SupportBundleEnvironmentRedactionTests
{
    private static readonly object EnvMutationLock = new();

    [Fact]
    public void SnapshotEnvironmentForBundle_masks_archiforge_key_containing_sql_even_when_value_looks_like_connection_string()
    {
        string suffix = Guid.NewGuid().ToString("N")[..10];
        string key = "ARCHIFORGE_UNITTEST_SQL_" + suffix;

        try
        {
            Environment.SetEnvironmentVariable(key, "Server=evil;Password=supersecret;");

            IReadOnlyDictionary<string, string> snap = SupportBundleRedactor.SnapshotEnvironmentForBundle();

            snap.Should().ContainKey(key);
            snap[key].Should().Be("(set)");
            snap[key].Should().NotContain("evil");
            snap[key].Should().NotContain("supersecret");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public void SnapshotEnvironmentForBundle_redacts_archiforge_api_url_user_info()
    {
        lock (EnvMutationLock)
        {
            string? prior = Environment.GetEnvironmentVariable("ARCHIFORGE_API_URL");

            try
            {
                Environment.SetEnvironmentVariable(
                    "ARCHIFORGE_API_URL",
                    "http://pilotuser:secretpass@10.0.0.1:8080/v1");

                IReadOnlyDictionary<string, string> snap = SupportBundleRedactor.SnapshotEnvironmentForBundle();

                snap.Should().ContainKey("ARCHIFORGE_API_URL");
                snap["ARCHIFORGE_API_URL"].Should().Be("http://10.0.0.1:8080/v1");
                snap["ARCHIFORGE_API_URL"].Should().NotContain("pilotuser");
                snap["ARCHIFORGE_API_URL"].Should().NotContain("secretpass");
            }
            finally
            {
                if (prior is null)
                {
                    Environment.SetEnvironmentVariable("ARCHIFORGE_API_URL", null);
                }
                else
                {
                    Environment.SetEnvironmentVariable("ARCHIFORGE_API_URL", prior);
                }
            }
        }
    }

    [Theory]
    [InlineData("ARCHIFORGE_CUSTOM_CONN_STRING")]
    [InlineData("ARCHIFORGE_MY_PASSWORD")]
    public void IsSensitiveEnvironmentVariableName_true_for_connection_and_password_patterns(string name)
    {
        SupportBundleRedactor.IsSensitiveEnvironmentVariableName(name).Should().BeTrue();
    }
}
