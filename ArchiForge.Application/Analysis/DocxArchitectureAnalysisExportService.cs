using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing;
using WpRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using DrRun = DocumentFormat.OpenXml.Drawing.Run;
using WpText = DocumentFormat.OpenXml.Wordprocessing.Text;
using DrText = DocumentFormat.OpenXml.Drawing.Text;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using WpRunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using DrRunProperties = DocumentFormat.OpenXml.Drawing.RunProperties;
using WpNonVisualGraphicFrameDrawingProperties =
    DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties;
using DocumentFormat.OpenXml.Drawing.Pictures;
using DrNonVisualPictureProperties = DocumentFormat.OpenXml.Drawing.NonVisualPictureProperties;
using DrPicNonVisualPictureProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties;
using DrNonVisualGraphicFrameDrawingProperties = DocumentFormat.OpenXml.Drawing.NonVisualGraphicFrameDrawingProperties;
using DrPicNonVisualDrawingProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties;
using DrPicNonVisualPictureDrawingProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties;
using DrPicture = DocumentFormat.OpenXml.Drawing.Picture;
using DrPicPicture = DocumentFormat.OpenXml.Drawing.Pictures.Picture;
using DrPicBlipFill = DocumentFormat.OpenXml.Drawing.Pictures.BlipFill;
using ArchiForge.Application.Diagrams;

namespace ArchiForge.Application.Analysis;

public sealed class DocxArchitectureAnalysisExportService : IArchitectureAnalysisDocxExportService
{
    private readonly IDiagramImageRenderer _diagramImageRenderer;

    public DocxArchitectureAnalysisExportService(IDiagramImageRenderer diagramImageRenderer)
    {
        _diagramImageRenderer = diagramImageRenderer;
    }

    public async Task<byte[]> GenerateDocxAsync(
        ArchitectureAnalysisReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

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

            if (report.Run.CompletedUtc.HasValue)
            {
                AddParagraph(body, $"Completed UTC: {report.Run.CompletedUtc.Value:O}");
            }

            if (!string.IsNullOrWhiteSpace(report.Run.CurrentManifestVersion))
            {
                AddParagraph(body, $"Current Manifest Version: {report.Run.CurrentManifestVersion}");
            }

            AddSpacer(body);

            if (report.Evidence is not null)
            {
                AddHeading(body, "Evidence Package", 2);

                AddParagraph(body, $"Evidence Package ID: {report.Evidence.EvidencePackageId}");
                AddParagraph(body, $"System Name: {report.Evidence.SystemName}");
                AddParagraph(body, $"Environment: {report.Evidence.Environment}");
                AddParagraph(body, $"Cloud Provider: {report.Evidence.CloudProvider}");

                AddSpacer(body);

                AddHeading(body, "Request Context", 3);
                AddParagraph(body, report.Evidence.Request.Description);

                if (report.Evidence.Request.Constraints.Count > 0)
                {
                    AddHeading(body, "Constraints", 3);
                    foreach (var item in report.Evidence.Request.Constraints)
                    {
                        AddBullet(body, item);
                    }
                }

                if (report.Evidence.Request.RequiredCapabilities.Count > 0)
                {
                    AddHeading(body, "Required Capabilities", 3);
                    foreach (var item in report.Evidence.Request.RequiredCapabilities)
                    {
                        AddBullet(body, item);
                    }
                }

                AddSpacer(body);
            }

            if (report.Manifest is not null)
            {
                AddHeading(body, "Architecture Manifest", 2);

                AddParagraph(body, $"System Name: {report.Manifest.SystemName}");
                AddParagraph(body, $"Run ID: {report.Manifest.RunId}");
                AddParagraph(body, $"Manifest Version: {report.Manifest.Metadata.ManifestVersion}");
                AddParagraph(body, $"Service Count: {report.Manifest.Services.Count}");
                AddParagraph(body, $"Datastore Count: {report.Manifest.Datastores.Count}");
                AddParagraph(body, $"Relationship Count: {report.Manifest.Relationships.Count}");

                AddSpacer(body);

                if (report.Manifest.Services.Count > 0)
                {
                    AddHeading(body, "Services", 3);

                    foreach (var service in report.Manifest.Services.OrderBy(x => x.ServiceName))
                    {
                        AddParagraph(body, service.ServiceName, bold: true);
                        AddBullet(body, $"Type: {service.ServiceType}");
                        AddBullet(body, $"Platform: {service.RuntimePlatform}");

                        if (!string.IsNullOrWhiteSpace(service.Purpose))
                        {
                            AddBullet(body, $"Purpose: {service.Purpose}");
                        }

                        if (service.RequiredControls.Count > 0)
                        {
                            AddBullet(body, $"Required Controls: {string.Join(", ", service.RequiredControls)}");
                        }
                    }

                    AddSpacer(body);
                }

                if (report.Manifest.Datastores.Count > 0)
                {
                    AddHeading(body, "Datastores", 3);

                    foreach (var datastore in report.Manifest.Datastores.OrderBy(x => x.DatastoreName))
                    {
                        AddParagraph(body, datastore.DatastoreName, bold: true);
                        AddBullet(body, $"Type: {datastore.DatastoreType}");
                        AddBullet(body, $"Platform: {datastore.RuntimePlatform}");
                        AddBullet(body, $"Private Endpoint Required: {(datastore.PrivateEndpointRequired ? "Yes" : "No")}");
                        AddBullet(body, $"Encryption At Rest Required: {(datastore.EncryptionAtRestRequired ? "Yes" : "No")}");
                    }

                    AddSpacer(body);
                }
            }

            if (!string.IsNullOrWhiteSpace(report.Diagram))
            {
                AddHeading(body, "Architecture Diagram", 2);

                var diagramBytes = await _diagramImageRenderer.RenderMermaidPngAsync(
                    report.Diagram,
                    cancellationToken);

                if (diagramBytes is not null && diagramBytes.Length > 0)
                {
                    AddImageToBody(mainPart, body, diagramBytes, "Architecture Diagram");
                }
                else
                {
                    AddParagraph(body, "Diagram image rendering was not available. Mermaid source is included below.");
                    AddCodeBlock(body, report.Diagram, "mermaid");
                }

                AddSpacer(body);
            }

            if (!string.IsNullOrWhiteSpace(report.Summary))
            {
                AddHeading(body, "Architecture Summary", 2);
                AddMultilineParagraphs(body, report.Summary);
                AddSpacer(body);
            }

            if (report.Determinism is not null)
            {
                AddHeading(body, "Determinism Check", 2);

                AddParagraph(body, $"Source Run ID: {report.Determinism.SourceRunId}");
                AddParagraph(body, $"Iterations: {report.Determinism.Iterations}");
                AddParagraph(body, $"Execution Mode: {report.Determinism.ExecutionMode}");
                AddParagraph(body, $"Is Deterministic: {(report.Determinism.IsDeterministic ? "Yes" : "No")}");
                AddParagraph(body, $"Baseline Replay Run ID: {report.Determinism.BaselineReplayRunId}");

                AddSpacer(body);

                foreach (var iteration in report.Determinism.IterationResults.OrderBy(x => x.IterationNumber))
                {
                    AddParagraph(body, $"Iteration {iteration.IterationNumber}", bold: true);
                    AddBullet(body, $"Replay Run ID: {iteration.ReplayRunId}");
                    AddBullet(body, $"Matches Baseline Agent Results: {(iteration.MatchesBaselineAgentResults ? "Yes" : "No")}");
                    AddBullet(body, $"Matches Baseline Manifest: {(iteration.MatchesBaselineManifest ? "Yes" : "No")}");

                    foreach (var warning in iteration.AgentDriftWarnings)
                    {
                        AddBullet(body, $"Agent Drift Warning: {warning}");
                    }

                    foreach (var warning in iteration.ManifestDriftWarnings)
                    {
                        AddBullet(body, $"Manifest Drift Warning: {warning}");
                    }

                    AddSpacer(body);
                }
            }

            if (report.ManifestDiff is not null)
            {
                AddHeading(body, "Manifest Diff", 2);
                AddDiffSection(body, "Added Services", report.ManifestDiff.AddedServices);
                AddDiffSection(body, "Removed Services", report.ManifestDiff.RemovedServices);
                AddDiffSection(body, "Added Datastores", report.ManifestDiff.AddedDatastores);
                AddDiffSection(body, "Removed Datastores", report.ManifestDiff.RemovedDatastores);
                AddDiffSection(body, "Added Required Controls", report.ManifestDiff.AddedRequiredControls);
                AddDiffSection(body, "Removed Required Controls", report.ManifestDiff.RemovedRequiredControls);
                AddSpacer(body);
            }

            if (report.AgentResultDiff is not null)
            {
                AddHeading(body, "Agent Result Diff", 2);

                foreach (var delta in report.AgentResultDiff.AgentDeltas.OrderBy(x => x.AgentType))
                {
                    AddParagraph(body, delta.AgentType.ToString(), bold: true);

                    AddBullet(body, $"Left Exists: {(delta.LeftExists ? "Yes" : "No")}");
                    AddBullet(body, $"Right Exists: {(delta.RightExists ? "Yes" : "No")}");
                    AddBullet(body, $"Left Confidence: {(delta.LeftConfidence.HasValue ? delta.LeftConfidence.Value.ToString("0.00") : "n/a")}");
                    AddBullet(body, $"Right Confidence: {(delta.RightConfidence.HasValue ? delta.RightConfidence.Value.ToString("0.00") : "n/a")}");

                    AddDiffSection(body, "Added Claims", delta.AddedClaims);
                    AddDiffSection(body, "Removed Claims", delta.RemovedClaims);
                    AddDiffSection(body, "Added Findings", delta.AddedFindings);
                    AddDiffSection(body, "Removed Findings", delta.RemovedFindings);
                    AddDiffSection(body, "Added Required Controls", delta.AddedRequiredControls);
                    AddDiffSection(body, "Removed Required Controls", delta.RemovedRequiredControls);

                    AddSpacer(body);
                }
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static void AddHeading(Body body, string text, int level)
    {
        body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
            new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(
                new ParagraphStyleId { Val = $"Heading{level}" }),
            new WpRun(new WpText(text))));
    }

    private static void AddParagraph(Body body, string text, bool bold = false)
    {
        var run = new WpRun(new WpText(text) { Space = SpaceProcessingModeValues.Preserve });
        
        if (bold)
        {
            run.RunProperties = new WpRunProperties(new Bold());
        }

        body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(run));
    }

    private static void AddBullet(Body body, string text)
    {
        body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
            new WpRun(new WpText($"• {text}") { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddSpacer(Body body)
    {
        body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new WpRun(new WpText(string.Empty))));
    }

    private static void AddMultilineParagraphs(Body body, string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');

        foreach (var line in lines)
        {
            AddParagraph(body, line);
        }
    }

    private static void AddCodeBlock(Body body, string text, string language)
    {
        AddParagraph(body, $"[{language}]");

        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var run = new WpRun(new WpText(line) { Space = SpaceProcessingModeValues.Preserve })
            {
                RunProperties = new WpRunProperties(new RunFonts { Ascii = "Consolas" })
            };

            body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(run));
        }
    }

    private static void AddDiffSection(Body body, string title, IReadOnlyCollection<string> items)
    {
        AddParagraph(body, title, bold: true);

        if (items.Count == 0)
        {
            AddBullet(body, "None");
            return;
        }

        foreach (var item in items)
        {
            AddBullet(body, item);
        }
    }

    private static void AddImageToBody(
        MainDocumentPart mainPart,
        Body body,
        byte[] imageBytes,
        string imageName)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);

        using (var stream = new MemoryStream(imageBytes))
        {
            imagePart.FeedData(stream);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);

        const long widthEmus = 6_000_000L;
        const long heightEmus = 3_500_000L;

        var drawing = new Drawing(
            new Inline(
                new Extent { Cx = widthEmus, Cy = heightEmus },
                new EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L
                },
                new DocProperties
                {
                    Id = 1U,
                    Name = imageName
                },
                new WpNonVisualGraphicFrameDrawingProperties(
                    new GraphicFrameLocks { NoChangeAspect = true }),
                new Graphic(
                    new GraphicData(
                        new DrPicPicture(
                            new DrPicNonVisualPictureProperties(
                                new DrPicNonVisualDrawingProperties
                                {
                                    Id = 0U,
                                    Name = imageName
                                },
                                new DrPicNonVisualPictureDrawingProperties()),
                            new DrPicBlipFill(
                                new Blip { Embed = relationshipId },
                                new Stretch(new FillRectangle())),
                            new DocumentFormat.OpenXml.Drawing.ShapeProperties(
                                new Transform2D(
                                    new Offset { X = 0L, Y = 0L },
                                    new Extents { Cx = widthEmus, Cy = heightEmus }),
                                new PresetGeometry(new AdjustValueList())
                                {
                                    Preset = ShapeTypeValues.Rectangle
                                }))
                    )
                    {
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                    }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            });

        body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DrRun(drawing)));
    }
}
