using System.Text;
using ArchiForge.Contracts.Agents;

namespace ArchiForge.Application.Evidence;

public sealed class MarkdownEvidenceSummaryFormatter : IEvidenceSummaryFormatter
{
    public string FormatMarkdown(AgentEvidencePackage evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var sb = new StringBuilder();

        sb.AppendLine("## Evidence Context");
        sb.AppendLine();

        sb.AppendLine("### Request");
        sb.AppendLine();
        sb.AppendLine($"- System Name: {evidence.SystemName}");
        sb.AppendLine($"- Environment: {evidence.Environment}");
        sb.AppendLine($"- Cloud Provider: {evidence.CloudProvider}");
        sb.AppendLine($"- Description: {evidence.Request.Description}");

        if (evidence.Request.Constraints.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Constraints");
            sb.AppendLine();

            foreach (var constraint in evidence.Request.Constraints)
            {
                sb.AppendLine($"- {constraint}");
            }
        }

        if (evidence.Request.RequiredCapabilities.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Required Capabilities");
            sb.AppendLine();

            foreach (var capability in evidence.Request.RequiredCapabilities)
            {
                sb.AppendLine($"- {capability}");
            }
        }

        if (evidence.Request.Assumptions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Assumptions");
            sb.AppendLine();

            foreach (var assumption in evidence.Request.Assumptions)
            {
                sb.AppendLine($"- {assumption}");
            }
        }

        if (evidence.Policies.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Policy Evidence");
            sb.AppendLine();

            foreach (var policy in evidence.Policies.OrderBy(p => p.Title))
            {
                sb.AppendLine($"- **{policy.Title}**");
                sb.AppendLine($"  - Policy ID: {policy.PolicyId}");
                sb.AppendLine($"  - Summary: {policy.Summary}");

                if (policy.RequiredControls.Count > 0)
                {
                    sb.AppendLine($"  - Required Controls: {string.Join(", ", policy.RequiredControls)}");
                }

                if (policy.Tags.Count > 0)
                {
                    sb.AppendLine($"  - Tags: {string.Join(", ", policy.Tags)}");
                }
            }
        }

        if (evidence.ServiceCatalog.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Service Catalog Hints");
            sb.AppendLine();

            foreach (var service in evidence.ServiceCatalog.OrderBy(s => s.ServiceName))
            {
                sb.AppendLine($"- **{service.ServiceName}**");
                sb.AppendLine($"  - Category: {service.Category}");
                sb.AppendLine($"  - Summary: {service.Summary}");

                if (service.RecommendedUseCases.Count > 0)
                {
                    sb.AppendLine($"  - Recommended Use Cases: {string.Join(", ", service.RecommendedUseCases)}");
                }

                if (service.Tags.Count > 0)
                {
                    sb.AppendLine($"  - Tags: {string.Join(", ", service.Tags)}");
                }
            }
        }

        if (evidence.Patterns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Pattern Hints");
            sb.AppendLine();

            foreach (var pattern in evidence.Patterns.OrderBy(p => p.Name))
            {
                sb.AppendLine($"- **{pattern.Name}**");
                sb.AppendLine($"  - Pattern ID: {pattern.PatternId}");
                sb.AppendLine($"  - Summary: {pattern.Summary}");

                if (pattern.ApplicableCapabilities.Count > 0)
                {
                    sb.AppendLine($"  - Applicable Capabilities: {string.Join(", ", pattern.ApplicableCapabilities)}");
                }

                if (pattern.SuggestedServices.Count > 0)
                {
                    sb.AppendLine($"  - Suggested Services: {string.Join(", ", pattern.SuggestedServices)}");
                }
            }
        }

        if (evidence.PriorManifest is not null)
        {
            sb.AppendLine();
            sb.AppendLine("### Prior Manifest Context");
            sb.AppendLine();
            sb.AppendLine($"- Manifest Version: {evidence.PriorManifest.ManifestVersion}");
            sb.AppendLine($"- Summary: {evidence.PriorManifest.Summary}");

            if (evidence.PriorManifest.ExistingServices.Count > 0)
            {
                sb.AppendLine($"- Existing Services: {string.Join(", ", evidence.PriorManifest.ExistingServices)}");
            }

            if (evidence.PriorManifest.ExistingDatastores.Count > 0)
            {
                sb.AppendLine($"- Existing Datastores: {string.Join(", ", evidence.PriorManifest.ExistingDatastores)}");
            }

            if (evidence.PriorManifest.ExistingRequiredControls.Count > 0)
            {
                sb.AppendLine($"- Existing Required Controls: {string.Join(", ", evidence.PriorManifest.ExistingRequiredControls)}");
            }
        }

        if (evidence.Notes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Evidence Notes");
            sb.AppendLine();

            foreach (var note in evidence.Notes)
            {
                sb.AppendLine($"- **{note.NoteType}**: {note.Message}");
            }
        }

        return sb.ToString();
    }
}
