using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ArchiForge.Application.Analysis;

public sealed class DocxArchitectureAnalysisExportService : IArchitectureAnalysisDocxExportService
{
    public byte[] GenerateDocx(ArchitectureAnalysisReport report)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(
            stream,
            WordprocessingDocumentType.Document,
            true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var body = mainPart.Document.Body!;

            AddHeading(body, "ArchiForge Analysis Report", 1);

            AddParagraph(body, $"Run ID: {report.Run.RunId}");
            AddParagraph(body, $"Request ID: {report.Run.RequestId}");
            AddParagraph(body, $"Status: {report.Run.Status}");
            AddParagraph(body, $"Created UTC: {report.Run.CreatedUtc:O}");

            if (!string.IsNullOrWhiteSpace(report.Run.CurrentManifestVersion))
                AddParagraph(body, $"Manifest Version: {report.Run.CurrentManifestVersion}");

            AddSpacer(body);

            if (report.Evidence != null)
            {
                AddHeading(body, "Evidence Package", 2);

                AddParagraph(body, $"System Name: {report.Evidence.SystemName}");
                AddParagraph(body, $"Environment: {report.Evidence.Environment}");
                AddParagraph(body, $"Cloud Provider: {report.Evidence.CloudProvider}");

                AddSpacer(body);

                AddHeading(body, "Request Description", 3);
                AddParagraph(body, report.Evidence.Request.Description);

                if (report.Evidence.Request.Constraints.Count > 0)
                {
                    AddHeading(body, "Constraints", 3);

                    foreach (var item in report.Evidence.Request.Constraints)
                        AddBullet(body, item);
                }

                AddSpacer(body);
            }

            if (report.Manifest != null)
            {
                AddHeading(body, "Architecture Manifest", 2);

                AddParagraph(body, $"Services: {report.Manifest.Services.Count}");
                AddParagraph(body, $"Datastores: {report.Manifest.Datastores.Count}");
                AddParagraph(body, $"Relationships: {report.Manifest.Relationships.Count}");

                AddSpacer(body);

                if (report.Manifest.Services.Count > 0)
                {
                    AddHeading(body, "Services", 3);

                    foreach (var svc in report.Manifest.Services)
                    {
                        AddParagraph(body, svc.ServiceName, true);
                        AddBullet(body, $"Type: {svc.ServiceType}");
                        AddBullet(body, $"Platform: {svc.RuntimePlatform}");

                        if (!string.IsNullOrWhiteSpace(svc.Purpose))
                            AddBullet(body, $"Purpose: {svc.Purpose}");
                    }
                }

                AddSpacer(body);

                if (report.Manifest.Datastores.Count > 0)
                {
                    AddHeading(body, "Datastores", 3);

                    foreach (var db in report.Manifest.Datastores)
                    {
                        AddParagraph(body, db.DatastoreName, true);
                        AddBullet(body, $"Type: {db.DatastoreType}");
                        AddBullet(body, $"Platform: {db.RuntimePlatform}");
                        AddBullet(body, $"Private Endpoint Required: {db.PrivateEndpointRequired}");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(report.Summary))
            {
                AddHeading(body, "Architecture Summary", 2);
                AddParagraph(body, report.Summary);
            }

            if (report.Determinism != null)
            {
                AddHeading(body, "Determinism Check", 2);

                AddParagraph(body, $"Iterations: {report.Determinism.Iterations}");
                AddParagraph(body, $"Deterministic: {report.Determinism.IsDeterministic}");

                foreach (var iter in report.Determinism.IterationResults)
                {
                    AddParagraph(body, $"Iteration {iter.IterationNumber}", true);

                    AddBullet(body, $"Replay Run ID: {iter.ReplayRunId}");
                    AddBullet(body, $"Matches Agent Results: {iter.MatchesBaselineAgentResults}");
                    AddBullet(body, $"Matches Manifest: {iter.MatchesBaselineManifest}");
                }
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static void AddHeading(Body body, string text, int level)
    {
        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = $"Heading{level}" }),
            new Run(new Text(text))));
    }

    private static void AddParagraph(Body body, string text, bool bold = false)
    {
        var run = new Run(new Text(text));

        if (bold)
            run.RunProperties = new RunProperties(new Bold());

        body.AppendChild(new Paragraph(run));
    }

    private static void AddBullet(Body body, string text)
    {
        body.AppendChild(new Paragraph(
            new Run(new Text($"• {text}"))));
    }

    private static void AddSpacer(Body body)
    {
        body.AppendChild(new Paragraph(new Run(new Text(""))));
    }
}
