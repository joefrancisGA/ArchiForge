using System.Text;
using System.Text.Json;

namespace ArchiForge.Api.Tests;

public class IntegrationTestBase(ArchiForgeApiFactory factory) : IClassFixture<ArchiForgeApiFactory>
{
    protected readonly HttpClient Client = factory.CreateClient();

    protected StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    protected readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}
