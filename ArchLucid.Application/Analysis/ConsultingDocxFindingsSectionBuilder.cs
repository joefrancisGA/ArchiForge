using ArchLucid.Contracts.Agents;

using DocumentFormat.OpenXml.Wordprocessing;

namespace ArchLucid.Application.Analysis;

internal static class ConsultingDocxFindingsSectionBuilder
{
    public static void Add(Body body, ArchitectureAnalysisReport report)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Evidence and Constraints", 1);

        if (report.Evidence is null)
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, "No evidence package was available for this run.",
                "BodyText");

            return;
        }

        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Request Context", 2);
        ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, report.Evidence.Request.Description, "BodyText");

        if (report.Evidence.Request.Constraints.Count > 0)
        {
            ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Constraints", 2);

            foreach (string item in report.Evidence.Request.Constraints)

                ConsultingDocxOpenXmlPrimitives.AddBullet(body, item);
        }

        if (report.Evidence.Request.RequiredCapabilities.Count > 0)
        {
            ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Required Capabilities", 2);

            foreach (string item in report.Evidence.Request.RequiredCapabilities)

                ConsultingDocxOpenXmlPrimitives.AddBullet(body, item);
        }

        if (report.Evidence.Policies.Count <= 0)
            return;

        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Policy Evidence", 2);

        foreach (PolicyEvidence policy in report.Evidence.Policies.OrderBy(x => x.Title))
        {
            ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, policy.Title, "Strong");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Policy ID: {policy.PolicyId}");
            ConsultingDocxOpenXmlPrimitives.AddBullet(body, $"Summary: {policy.Summary}");

            if (policy.RequiredControls.Count > 0)

                ConsultingDocxOpenXmlPrimitives.AddBullet(body,
                    $"Required Controls: {string.Join(", ", policy.RequiredControls)}");
        }
    }
}
