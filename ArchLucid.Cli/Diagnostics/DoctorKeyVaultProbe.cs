using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Cli.Diagnostics;

/// <summary>
///     Optional Azure Key Vault connectivity probe for <c>archlucid doctor</c> (never throws to the caller).
/// </summary>
internal static class DoctorKeyVaultProbe
{
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    internal static DoctorKeyVaultProbePlan ResolvePlan(IConfiguration configuration)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        string provider = (configuration["ArchLucid:Secrets:Provider"] ?? string.Empty).Trim();
        string? uriRaw = configuration["ArchLucid:Secrets:KeyVaultUri"]?.Trim();
        bool providerIsKeyVault = string.Equals(provider, "KeyVault", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(uriRaw))
        {
            if (providerIsKeyVault)
            {
                return new DoctorKeyVaultProbePlan(
                    DoctorKeyVaultProbePlan.Decision.BadConfiguration,
                    "Key Vault: Misconfigured — ArchLucid:Secrets:Provider is KeyVault but ArchLucid:Secrets:KeyVaultUri is empty.",
                    vaultUri: null,
                    optionalSecretName: null);
            }

            return new DoctorKeyVaultProbePlan(
                DoctorKeyVaultProbePlan.Decision.Skip,
                "Key Vault: Skipped — ArchLucid:Secrets:KeyVaultUri is not set (export ArchLucid__Secrets__KeyVaultUri or add it to appsettings).",
                vaultUri: null,
                optionalSecretName: null);
        }

        if (!Uri.TryCreate(uriRaw, UriKind.Absolute, out Uri? vaultUri)
            || !string.Equals(vaultUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return new DoctorKeyVaultProbePlan(
                DoctorKeyVaultProbePlan.Decision.BadConfiguration,
                $"Key Vault: Invalid configuration — '{uriRaw}' must be an absolute https URI.",
                vaultUri: null,
                optionalSecretName: null);
        }

        string? probeSecret =
            Environment.GetEnvironmentVariable("ARCHLUCID_DOCTOR_KEYVAULT_PROBE_SECRET")?.Trim();

        if (string.IsNullOrWhiteSpace(probeSecret))
            probeSecret = null;

        return new DoctorKeyVaultProbePlan(
            DoctorKeyVaultProbePlan.Decision.Ready,
            string.Empty,
            vaultUri,
            probeSecret);
    }

    internal static string DescribeFailure(Exception exception, bool probeWasGetSecret)
    {
        Exception root = Unwrap(exception);

        if (root is OperationCanceledException)
            return "Key Vault: Timed out — check network, Private Link DNS, firewall, or run doctor from a reachable host.";

        if (root is not RequestFailedException requestFailed)
            return root is CredentialUnavailableException
                ? "Key Vault: Authentication Failed (Check Managed Identity, run `az login`, or set service principal env vars for DefaultAzureCredential)."
                : $"Key Vault: Error ({root.GetType().Name}) — {root.Message}";

        if (requestFailed.Status == 403)
        {
            return probeWasGetSecret
                ? "Key Vault: Permission Denied (Missing Key Vault Secrets User or secrets/get permission)."
                : "Key Vault: Permission Denied (Missing Key Vault Secrets User or secrets/list permission).";
        }

        if (requestFailed.Status == 401)
            return "Key Vault: Authentication Failed (credential rejected — check tenant, audience, or vault URL).";

        return root is CredentialUnavailableException ? "Key Vault: Authentication Failed (Check Managed Identity, run `az login`, or set service principal env vars for DefaultAzureCredential)." : $"Key Vault: Error ({root.GetType().Name}) — {root.Message}";
    }

    /// <summary>
    ///     Writes the probe section. Failures are summarized inline; never throws except for argument validation.
    /// </summary>
    internal static async Task WriteSectionAsync(TextWriter writer, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(configuration);

        await writer.WriteLineAsync("--- Azure Key Vault (local env + optional appsettings.json in cwd) ---");
        await writer.WriteLineAsync(
            "Uses DefaultAzureCredential (managed identity on Azure, Azure CLI / developer creds locally, or a service principal via AZURE_CLIENT_ID / AZURE_TENANT_ID / AZURE_CLIENT_SECRET).");
        await writer.WriteLineAsync(
            "Optional: set ARCHLUCID_DOCTOR_KEYVAULT_PROBE_SECRET to a secret name to verify read (value is never printed).");

        DoctorKeyVaultProbePlan plan = ResolvePlan(configuration);

        if (plan.DecisionKind is DoctorKeyVaultProbePlan.Decision.Skip or DoctorKeyVaultProbePlan.Decision.BadConfiguration)
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(plan.Message);
            await writer.WriteLineAsync();

            return;
        }

        Uri? vaultUri = plan.VaultUri;

        if (vaultUri is null)
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("Key Vault: Internal error — probe plan missing vault URI.");
            await writer.WriteLineAsync();

            return;
        }

        try
        {
            using CancellationTokenSource timeoutCts = new();
            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            timeoutCts.CancelAfter(DefaultTimeout);

            SecretClient client = new(vaultUri, new DefaultAzureCredential());
            bool getSecret = plan.OptionalSecretName is not null;

            if (getSecret)
            {
                _ = await client.GetSecretAsync(plan.OptionalSecretName, cancellationToken: linked.Token)
                    .ConfigureAwait(false);

                await writer.WriteLineAsync();
                await writer.WriteLineAsync(
                    $"Key Vault: Connected — read metadata for secret '{plan.OptionalSecretName}' OK (value not shown).");
            }
            else
            {
                int count = 0;

                await foreach (SecretProperties _ in client
                                   .GetPropertiesOfSecretsAsync(linked.Token)
                                   .ConfigureAwait(false))
                {
                    count++;

                    if (count >= 1)
                        break;
                }

                await writer.WriteLineAsync();
                await writer.WriteLineAsync(
                    $"Key Vault: Connected — list secrets reached vault '{vaultUri.Host}' (first page only; names not shown).");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(DescribeFailure(new OperationCanceledException("probe budget exhausted"),
                probeWasGetSecret: plan.OptionalSecretName is not null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(DescribeFailure(ex, probeWasGetSecret: plan.OptionalSecretName is not null));
        }

        await writer.WriteLineAsync();
    }

    private static Exception Unwrap(Exception exception)
    {
        if (exception is AggregateException { InnerExceptions.Count: 1 } aggregate)
            return aggregate.InnerExceptions[0];

        return exception;
    }
}
