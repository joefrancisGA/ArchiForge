using DocumentFormat.OpenXml.Wordprocessing;

namespace ArchLucid.Application.Analysis;

internal static class ConsultingDocxRecommendationsSectionBuilder
{
    public static void Add(Body body, ArchitectureAnalysisReport report, ConsultingDocxTemplateOptions options)
    {
        ConsultingDocxOpenXmlPrimitives.AddHeading(body, "Conclusions", 1);

        ConsultingDocxOpenXmlPrimitives.AddStyledParagraph(body, options.ConclusionsText, "BodyText");

        if (report.Warnings.Count <= 0)
            return;

        ConsultingDocxOpenXmlPrimitives.AddSpacer(body);
        ConsultingDocxOpenXmlPrimitives.AddCallout(body,
            "Open warnings remain and should be resolved or explicitly accepted.", options);
    }
}
