using ArchLucid.Cli.Support;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
///     Support bundle environment snapshot: secret names and URL redaction (56R).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class SupportBundleEnvironmentRedactionTests
{
    private static readonly object EnvMutationLock = new();

    [Fact]
    public void
        SnapshotEnvironmentForBundle_masks_archlucid_prefixed_key_containing_sql_even_when_value_looks_like_connection_string()
    {
        string suffix = Guid.NewGuid().ToString("N")[..10];
        string key = "ARCHLUCID_UNITTEST_SQL_" + suffix;

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
    public void SnapshotEnvironmentForBundle_redacts_archlucid_api_url_user_info()
    {
        lock (EnvMutationLock)
        {
            string? prior = Environment.GetEnvironmentVariable("ARCHLUCID_API_URL");

            try
            {
                Environment.SetEnvironmentVariable(
                    "ARCHLUCID_API_URL",
                    "http://pilotuser:secretpass@10.0.0.1:8080/v1");

                IReadOnlyDictionary<string, string> snap = SupportBundleRedactor.SnapshotEnvironmentForBundle();

                snap.Should().ContainKey("ARCHLUCID_API_URL");
                snap["ARCHLUCID_API_URL"].Should().Be("http://10.0.0.1:8080/v1");
                snap["ARCHLUCID_API_URL"].Should().NotContain("pilotuser");
                snap["ARCHLUCID_API_URL"].Should().NotContain("secretpass");
            }
            finally
            {
                if (prior is null)

                    Environment.SetEnvironmentVariable("ARCHLUCID_API_URL", null);

                else

                    Environment.SetEnvironmentVariable("ARCHLUCID_API_URL", prior);
            }
        }
    }

    [Theory]
    [InlineData("ARCHLUCID_CUSTOM_CONN_STRING")]
    [InlineData("ARCHLUCID_MY_PASSWORD")]
    public void IsSensitiveEnvironmentVariableName_true_for_connection_and_password_patterns(string name)
    {
        SupportBundleRedactor.IsSensitiveEnvironmentVariableName(name).Should().BeTrue();
    }
}
