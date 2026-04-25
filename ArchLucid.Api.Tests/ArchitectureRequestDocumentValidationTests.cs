using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Ensures inline document <c>contentType</c> is rejected at the API boundary when not in
///     <see cref="ArchLucid.ContextIngestion.SupportedContextDocumentContentTypes" />.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureRequestDocumentValidationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateRun_InvalidDocumentContentType_Returns400_WithFieldScopedError()
    {
        JsonNode root = JsonSerializer.SerializeToNode(
            TestRequestFactory.CreateArchitectureRequest("REQ-DOC-CTYPE-400"),
            JsonOptions)!.AsObject();

        root["documents"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "bad.bin",
                ["contentType"] = "application/pdf",
                ["content"] = "not a supported document body"
            }
        };

        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(root));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(body);

        JsonElement rootEl = doc.RootElement;
        rootEl.TryGetProperty("errors", out JsonElement errors).Should().BeTrue(
            "validation failures should expose an errors object (problem details shape)");

        bool hasDocumentContentTypeKey = false;

        foreach (JsonProperty p in errors.EnumerateObject())
        {
            if (!p.Name.Contains("contentType", StringComparison.OrdinalIgnoreCase) ||
                !p.Name.Contains("document", StringComparison.OrdinalIgnoreCase))
                continue;

            hasDocumentContentTypeKey = true;
            break;
        }

        hasDocumentContentTypeKey.Should().BeTrue(
            "expected a validation key scoped to documents and content type, e.g. documents[0].contentType; got keys: {0}",
            string.Join(", ", errors.EnumerateObject().Select(p => p.Name)));
    }
}
