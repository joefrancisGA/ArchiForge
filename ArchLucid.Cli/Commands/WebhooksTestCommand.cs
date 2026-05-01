using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Posts a sample outbound webhook JSON body to a receiver URL, optionally signing with the same HMAC header as
///     API webhook delivery (<see cref="WebhookSignatureHeaderName" />).
/// </summary>
[ExcludeFromCodeCoverage(Justification =
    "CLI webhook probe reaches arbitrary HTTPS URLs; validated via Program integration tests for usage paths.")]
internal static class WebhooksTestCommand
{
    /// <summary>Mirror of Host.Core <c>WebhookSignature.HeaderName</c> (CLI cannot reference Host.Core).</summary>
    internal const string WebhookSignatureHeaderName = "X-ArchLucid-Webhook-Signature";

    internal const string WebhookSignaturePrefix = "sha256=";

    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<int> RunAsync(string[] args)
    {
        string? url = Environment.GetEnvironmentVariable("ARCHLUCID_WEBHOOK_TEST_URL")?.Trim();
        string? secret = Environment.GetEnvironmentVariable("ARCHLUCID_WEBHOOK_TEST_SECRET")?.Trim();
        string? payloadPath = null;
        bool jsonMachineOutput = CliExecutionContext.JsonOutput;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            if (string.Equals(a, "--url", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                url = args[++i].Trim();
                continue;
            }

            if (string.Equals(a, "--secret", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                secret = args[++i].Trim();
                continue;
            }

            if (string.Equals(a, "--payload", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                payloadPath = args[++i].Trim();
                continue;
            }

            if (string.Equals(a, "--help", StringComparison.Ordinal) ||
                string.Equals(a, "-h", StringComparison.Ordinal))
            {
                WriteUsage(true);

                return CliExitCode.Success;
            }

            WriteUsage(false);

            return CliExitCode.UsageError;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            WriteUsage(false);

            return CliExitCode.UsageError;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? absolute) ||
            (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps))
        {
            if (jsonMachineOutput)
            {
                CliJson.WriteFailureLine(
                    Console.Error,
                    CliExitCode.UsageError,
                    "invalid_url",
                    "Expected --url with an absolute http(s) URL.");
            }
            else
            {
                Console.Error.WriteLine("Expected --url with an absolute http(s) URL.");
            }

            return CliExitCode.UsageError;
        }

        byte[] bodyUtf8;

        try
        {
            bodyUtf8 = await LoadPayloadUtf8Async(payloadPath);
        }
        catch (Exception ex) when (ex is IOException or FileNotFoundException or ArgumentException)
        {
            if (jsonMachineOutput)
            {
                CliJson.WriteFailureLine(
                    Console.Error,
                    CliExitCode.UsageError,
                    "payload_error",
                    ex.Message);
            }
            else
            {
                Console.Error.WriteLine(ex.Message);
            }

            return CliExitCode.UsageError;
        }

        using HttpClient http = new();
        using HttpRequestMessage request = new(HttpMethod.Post, absolute);
        request.Content = new ByteArrayContent(bodyUtf8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        if (!string.IsNullOrWhiteSpace(secret))
        {
            string hex = ComputeSha256Hex(secret, bodyUtf8);
            request.Headers.TryAddWithoutValidation(WebhookSignatureHeaderName, WebhookSignaturePrefix + hex);
        }

        HttpResponseMessage response;

        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (Exception ex)
        {
            if (jsonMachineOutput)
            {
                CliJson.WriteFailureLine(
                    Console.Error,
                    CliExitCode.OperationFailed,
                    "transport_error",
                    ex.Message);
            }
            else
            {
                Console.Error.WriteLine($"Webhook POST failed: {ex.Message}");
            }

            return CliExitCode.OperationFailed;
        }

        using (response)
        {
            int status = (int)response.StatusCode;

            if (jsonMachineOutput)
            {
                object payload = new
                {
                    ok = response.IsSuccessStatusCode,
                    exitCode = response.IsSuccessStatusCode ? CliExitCode.Success : CliExitCode.OperationFailed,
                    statusCode = status,
                    reasonPhrase = response.ReasonPhrase
                };

                Console.WriteLine(JsonSerializer.Serialize(payload, JsonCamel));
            }
            else
            {
                Console.WriteLine($"HTTP {status} {response.ReasonPhrase}");
            }

            return response.IsSuccessStatusCode ? CliExitCode.Success : CliExitCode.OperationFailed;
        }
    }

    internal static void WriteUsage(bool stdout)
    {
        const string plain =
            "Usage: archlucid webhooks test --url <absolute-http-url> [--secret <shared-secret>] [--payload <path.json>] "
            + "| [--help]\n"
            + "Environment (optional defaults): ARCHLUCID_WEBHOOK_TEST_URL, ARCHLUCID_WEBHOOK_TEST_SECRET.\n"
            + "Signs the UTF-8 POST body with HMAC-SHA256 using header "
            + WebhookSignatureHeaderName
            + " when --secret or ARCHLUCID_WEBHOOK_TEST_SECRET is set (matches API webhook delivery).";

        if (CliExecutionContext.JsonOutput && !stdout)
        {
            CliJson.WriteFailureLine(Console.Error, CliExitCode.UsageError, "usage", plain);

            return;
        }

        TextWriter w = stdout ? Console.Out : Console.Error;
        w.WriteLine(plain);
    }

    /// <remarks>
    ///     Duplicates Host.Core <c>WebhookSignature.ComputeSha256Hex</c> so the CLI stays dependency-light.
    /// </remarks>
    private static string ComputeSha256Hex(string sharedSecret, byte[] utf8Body)
    {
        if (string.IsNullOrEmpty(sharedSecret))
            throw new ArgumentException("Shared secret is required.", nameof(sharedSecret));

        byte[] key = Encoding.UTF8.GetBytes(sharedSecret);

        using HMACSHA256 hmac = new(key);
        byte[] hash = hmac.ComputeHash(utf8Body);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<byte[]> LoadPayloadUtf8Async(string? payloadPath)
    {
        if (!string.IsNullOrWhiteSpace(payloadPath))
        {
            if (!File.Exists(payloadPath))
                throw new FileNotFoundException($"Payload file not found: {payloadPath}", payloadPath);

            return await File.ReadAllBytesAsync(payloadPath);
        }

        // Minimal CloudEvents-shaped JSON aligned with schemas/events/finding-created.sample.schema.json.
        Dictionary<string, object?> envelope = new()
        {
            ["specversion"] = "1.0",
            ["type"] = "com.archlucid.finding.created.sample",
            ["source"] = "https://cli.archlucid.local/webhooks/test",
            ["id"] = Guid.NewGuid().ToString("D"),
            ["time"] = DateTime.UtcNow.ToString("O"),
            ["datacontenttype"] = "application/json",
            ["data"] = new Dictionary<string, object?>
            {
                ["tenantId"] = Guid.Empty.ToString("D"),
                ["findingId"] = Guid.NewGuid().ToString("D"),
                ["runId"] = Guid.NewGuid().ToString("D"),
                ["note"] =
                    "Synthetic CLI probe — replace with a captured production envelope before customer hand-off."
            }
        };

        string json = JsonSerializer.Serialize(envelope, JsonCamel);

        return Encoding.UTF8.GetBytes(json);
    }
}
