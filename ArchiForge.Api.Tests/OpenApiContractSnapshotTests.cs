using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Mvc.Testing;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Fails when the ASP.NET Core OpenAPI document (<c>MapOpenApi</c>, <c>/openapi/v1.json</c>) drifts from the committed snapshot.
/// Swashbuckle <c>/swagger/v1/swagger.json</c> is covered by generation smoke tests; this snapshot uses the Microsoft OpenAPI document for stable contract diffing.
/// Regenerate: <c>ARCHIFORGE_UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApiContractSnapshotTests</c> from repo root.
/// </summary>
[Trait("Suite", "Core")]
public sealed class OpenApiContractSnapshotTests : IClassFixture<OpenApiContractWebAppFactory>
{
    private const string OpenApiDocumentPath = "/openapi/v1.json";
    private const string SnapshotFileName = "openapi-v1.contract.snapshot.json";

    private readonly OpenApiContractWebAppFactory _factory;

    public OpenApiContractSnapshotTests(OpenApiContractWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task OpenApi_v1_json_matches_committed_snapshot()
    {
        using HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using HttpResponseMessage response = await client.GetAsync(OpenApiDocumentPath);
        response.EnsureSuccessStatusCode();

        string actual = await response.Content.ReadAsStringAsync();
        JsonNode? actualNode = JsonNode.Parse(actual);
        Assert.NotNull(actualNode);

        if (string.Equals(
                Environment.GetEnvironmentVariable("ARCHIFORGE_UPDATE_OPENAPI_SNAPSHOT"),
                "1",
                StringComparison.Ordinal))
        {
            string path = ResolveSourceSnapshotPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            string normalized = JsonSerializer.Serialize(
                actualNode,
                new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(path, normalized);
            return;
        }

        string snapshotOnDisk = Path.Combine(AppContext.BaseDirectory, "Contracts", "openapi-v1.swagger.snapshot.json");
        Assert.True(
            File.Exists(snapshotOnDisk),
            $"Missing snapshot at {snapshotOnDisk}. Run once with ARCHIFORGE_UPDATE_OPENAPI_SNAPSHOT=1 to generate.");

        string expectedJson = await File.ReadAllTextAsync(snapshotOnDisk);
        JsonNode? expectedNode = JsonNode.Parse(expectedJson);
        Assert.NotNull(expectedNode);

        if (!JsonNode.DeepEquals(actualNode, expectedNode))
        {
            Assert.Fail(
                $"OpenAPI document drifted from Contracts/{SnapshotFileName}. " +
                "Review API changes, then regenerate with ARCHIFORGE_UPDATE_OPENAPI_SNAPSHOT=1.");
        }
    }

    private static string ResolveSourceSnapshotPath()
    {
        string assemblyFile = typeof(OpenApiContractSnapshotTests).Assembly.Location;
        string? dir = Path.GetDirectoryName(assemblyFile);

        for (int i = 0; i < 12 && dir != null; i++)
        {
            string csproj = Path.Combine(dir, "ArchiForge.Api.Tests.csproj");

            if (File.Exists(csproj))
            {
                return Path.Combine(dir, "Contracts", SnapshotFileName);
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Cannot find ArchiForge.Api.Tests.csproj for OpenAPI snapshot path.");
    }
}
