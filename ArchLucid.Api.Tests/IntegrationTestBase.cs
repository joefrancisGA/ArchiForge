using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Core.Scoping;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Base for API integration tests: provides an <see cref="HttpClient"/> from <see cref="ArchLucidApiFactory"/> and JSON helpers aligned with the API’s serializer settings.
/// </summary>
public class IntegrationTestBase(ArchLucidApiFactory factory) : IClassFixture<ArchLucidApiFactory>
{
    /// <summary>Factory for the hosted API (singleton services, SQL connection string, etc.).</summary>
    protected ArchLucidApiFactory Factory { get; } = factory;

    /// <summary>
    /// DevelopmentBypass authentication does not emit <c>tenant_id</c> claims; scope headers align the client with
    /// <see cref="ScopeIds"/> defaults so SQL-backed <c>CommercialTenantTierFilter</c> can resolve <c>dbo.Tenants</c>.
    /// </summary>
    protected readonly HttpClient Client = CreateClientWithDefaultScopeHeaders(factory);

    /// <summary>
    /// Serializes <paramref name="value"/> with <see cref="JsonOptions"/> and returns <see cref="StringContent"/> suitable for <c>application/json</c> POST bodies.
    /// </summary>
    protected StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>Aligned with <see cref="ArchLucid.Api.Startup.MvcExtensions"/> API JSON options (camelCase properties, string enums).</summary>
    protected readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true) },
    };

    private static HttpClient CreateClientWithDefaultScopeHeaders(ArchLucidApiFactory apiFactory)
    {
        HttpClient client = apiFactory.CreateClient();
        WireDefaultSqlIntegrationScopeHeaders(client);

        return client;
    }

    /// <summary>
    /// Adds <c>x-tenant-id</c> / <c>x-workspace-id</c> / <c>x-project-id</c> for DevelopmentBypass + SQL integration hosts.
    /// </summary>
    public static void WireDefaultSqlIntegrationScopeHeaders(HttpClient client)
    {
        _ = client.DefaultRequestHeaders.TryAddWithoutValidation("x-tenant-id", ScopeIds.DefaultTenant.ToString("D"));
        _ = client.DefaultRequestHeaders.TryAddWithoutValidation("x-workspace-id", ScopeIds.DefaultWorkspace.ToString("D"));
        _ = client.DefaultRequestHeaders.TryAddWithoutValidation("x-project-id", ScopeIds.DefaultProject.ToString("D"));
    }
}
