using ArchLucid.Cli.Real;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

public sealed class RealModePreflightTests
{
    private static readonly object AzureEnvLock = new();

    private const string Endpoint = "AZURE_OPENAI_ENDPOINT";
    private const string ApiKey = "AZURE_OPENAI_API_KEY";
    private const string Deployment = "AZURE_OPENAI_DEPLOYMENT_NAME";

    private static void SaveAndSet(string key, string? value, Dictionary<string, string?> stash)
    {
        if (!stash.ContainsKey(key))
            stash[key] = Environment.GetEnvironmentVariable(key);

        if (value is null)
            Environment.SetEnvironmentVariable(key, null);
        else
            Environment.SetEnvironmentVariable(key, value);
    }

    private static void RestoreAll(Dictionary<string, string?> stash)
    {
        foreach (KeyValuePair<string, string?> kv in stash)
        {
            if (kv.Value is null)
                Environment.SetEnvironmentVariable(kv.Key, null);
            else
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
        }
    }

    [Fact]
    public void Validate_WhenAllThreeSet_returnsOk()
    {
        lock (AzureEnvLock)
        {
            Dictionary<string, string?> stash = new();

            try
            {
                SaveAndSet(Endpoint, "https://x.openai.azure.com/", stash);
                SaveAndSet(ApiKey, "k", stash);
                SaveAndSet(Deployment, "d", stash);

                RealModePreflightResult r = RealModePreflight.Validate();

                r.IsOk.Should().BeTrue();
                r.MissingKeys.Should().BeEmpty();
                r.ErrorMessage.Should().BeNull();
            }
            finally
            {
                RestoreAll(stash);
            }
        }
    }

    [Fact]
    public void Validate_WhenAllMissing_returnsAllKeysAndMessage()
    {
        lock (AzureEnvLock)
        {
            Dictionary<string, string?> stash = new();

            try
            {
                SaveAndSet(Endpoint, null, stash);
                SaveAndSet(ApiKey, null, stash);
                SaveAndSet(Deployment, null, stash);

                RealModePreflightResult r = RealModePreflight.Validate();

                r.IsOk.Should().BeFalse();
                r.MissingKeys.Should().BeEquivalentTo(Endpoint, ApiKey, Deployment);
                r.ErrorMessage.Should().Contain(Endpoint);
                r.ErrorMessage.Should().Contain(ApiKey);
                r.ErrorMessage.Should().Contain(Deployment);
            }
            finally
            {
                RestoreAll(stash);
            }
        }
    }

    [Fact]
    public void Validate_WhenOnlyEndpointMissing_listsEndpointOnly()
    {
        lock (AzureEnvLock)
        {
            Dictionary<string, string?> stash = new();

            try
            {
                SaveAndSet(Endpoint, null, stash);
                SaveAndSet(ApiKey, "key", stash);
                SaveAndSet(Deployment, "dep", stash);

                RealModePreflightResult r = RealModePreflight.Validate();

                r.IsOk.Should().BeFalse();
                r.MissingKeys.Should().BeEquivalentTo(Endpoint);
            }
            finally
            {
                RestoreAll(stash);
            }
        }
    }
}
