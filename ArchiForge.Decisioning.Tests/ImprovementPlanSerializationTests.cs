using System.Text.Json;

using ArchiForge.Decisioning.Advisory.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class ImprovementPlanSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PolicyPackAdvisoryDefaults_RoundTrips_Json()
    {
        ImprovementPlan plan = new ImprovementPlan
        {
            RunId = Guid.NewGuid(),
            GeneratedUtc = DateTime.UtcNow,
            PolicyPackAdvisoryDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["scanDepth"] = "deep",
                ["channel"] = "email",
            },
        };

        string json = JsonSerializer.Serialize(plan, Options);
        ImprovementPlan? back = JsonSerializer.Deserialize<ImprovementPlan>(json, Options);

        back.Should().NotBeNull();
        back.PolicyPackAdvisoryDefaults.Should().ContainKey("scanDepth");
        back.PolicyPackAdvisoryDefaults["scanDepth"].Should().Be("deep");
        back.PolicyPackAdvisoryDefaults["channel"].Should().Be("email");
    }
}
