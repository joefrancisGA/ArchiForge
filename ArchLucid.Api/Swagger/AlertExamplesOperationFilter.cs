using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

/// <summary>Markdown JSON examples for alert operator POST bodies (simulation, tuning, rule create).</summary>
public sealed class AlertExamplesOperationFilter : IOperationFilter
{
    private const string SimpleSimulationExample =
        """
        {
          "ruleKind": "Simple",
          "simpleRule": {
            "name": "High finding count",
            "ruleType": "FindingCount",
            "severity": "Warning",
            "thresholdValue": 10,
            "isEnabled": true,
            "targetChannelType": "DigestOnly",
            "metadataJson": "{}"
          },
          "recentRunCount": 5,
          "useHistoricalWindow": true,
          "runProjectSlug": "default"
        }
        """;

    private const string CompareCandidatesSimpleExample =
        """
        {
          "ruleKind": "Simple",
          "candidateASimpleRule": {
            "name": "Candidate A",
            "ruleType": "FindingCount",
            "severity": "Warning",
            "thresholdValue": 5,
            "metadataJson": "{}"
          },
          "candidateBSimpleRule": {
            "name": "Candidate B",
            "ruleType": "FindingCount",
            "severity": "Warning",
            "thresholdValue": 15,
            "metadataJson": "{}"
          },
          "recentRunCount": 5,
          "runProjectSlug": "default"
        }
        """;

    private const string ThresholdRecommendExample =
        """
        {
          "ruleKind": "Simple",
          "tunedMetricType": "FindingCount",
          "candidateThresholds": [ 3, 5, 8, 12 ],
          "recentRunCount": 10,
          "targetCreatedAlertCountMin": 1,
          "targetCreatedAlertCountMax": 5,
          "runProjectSlug": "default",
          "baseSimpleRule": {
            "name": "Baseline",
            "ruleType": "FindingCount",
            "severity": "Warning",
            "thresholdValue": 5,
            "metadataJson": "{}"
          }
        }
        """;

    private const string CreateAlertRuleExample =
        """
        {
          "name": "Finding count threshold",
          "ruleType": "FindingCount",
          "severity": "Warning",
          "thresholdValue": 7,
          "isEnabled": true,
          "targetChannelType": "DigestOnly",
          "metadataJson": "{}"
        }
        """;

    private const string CreateCompositeExample =
        """
        {
          "name": "Multi-metric gate",
          "severity": "Critical",
          "operator": "And",
          "suppressionWindowMinutes": 1440,
          "cooldownMinutes": 60,
          "reopenDeltaThreshold": 0,
          "dedupeScope": "RuleAndRun",
          "isEnabled": true,
          "targetChannelType": "AlertRouting",
          "conditions": [
            { "metricType": "FindingCount", "operator": "GreaterThan", "thresholdValue": 5 }
          ]
        }
        """;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor cad)
            return;

        string? example = (cad.ControllerName, cad.ActionName) switch
        {
            ("AlertSimulation", "Simulate") => SimpleSimulationExample,
            ("AlertSimulation", "CompareCandidates") => CompareCandidatesSimpleExample,
            ("AlertTuning", "RecommendThreshold") => ThresholdRecommendExample,
            ("AlertRules", "Create") => CreateAlertRuleExample,
            ("CompositeAlertRules", "Create") => CreateCompositeExample,
            _ => null,
        };

        if (example is null)
            return;

        string block = "\n\n### Example request body (JSON)\n\n```json\n" + example.Trim() + "\n```\n";
        string intro =
            "See **`docs/API_CONTRACTS.md`** (alerts / policy packs) for scope headers and governance filtering.\n\n";

        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? intro + block
            : operation.Description.TrimEnd() + "\n\n" + intro + block;
    }
}
